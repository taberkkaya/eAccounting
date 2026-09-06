using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace eAccountingServer.Application.Features.Accounting;

/// <summary>Kasa ya da banka farkı olmadan bir hesap.</summary>
public sealed record AccountInfo(Guid Id, string Name, AccountKind Kind, int CurrencyValue);

/// <summary>
/// Cariye, kasaya ve bankaya yazan tek yer.
///
/// Bir tahsilat iki tarafı birden değiştirir: carinin borcu azalır, kasaya para
/// girer. Bu ikisi ayrı ayrı yazılırsa er geç biri yazılıp diğeri yazılmaz ve
/// ekstre kasa bakiyesiyle çelişir. Tek kapıdan geçirmek, yazma ve geri alma
/// işini simetrik tutuyor: <c>Post*</c> ile yazılan her şey <c>Remove*</c> ile
/// aynı sırayla geri alınabiliyor.
///
/// Kayıtlar burada yalnızca hazırlanıyor; kaydetmek çağıranın işi, çünkü bir
/// faturanın cari, stok ve kasa etkisi tek bir SaveChanges'te inmeli.
/// </summary>
internal sealed class AccountingLedger(
    ICashRegisterRepository cashRegisterRepository,
    ICashRegisterDetailRepository cashRegisterDetailRepository,
    IBankRepository bankRepository,
    IBankDetailRepository bankDetailRepository,
    IContactRepository contactRepository,
    IContactTransactionRepository contactTransactionRepository,
    ICacheService cacheService)
{
    /// <summary>
    /// Hesap listeleri önbellekten okunuyor. Bakiyeyi değiştirip önbelleği
    /// bırakmak, tahsilatı yapıp kasada eski rakamı göstermek demek: kayıt
    /// doğru, ekran yanlış olur.
    /// </summary>
    private void InvalidateAccountCache(AccountKind kind) =>
        cacheService.Remove(kind == AccountKind.CashRegister ? "cashRegisters" : "banks");

    // --- hesap tarafı -------------------------------------------------------

    /// <summary>Hesabı bulur; yoksa null döner. Tür verilmezse ikisine de bakar.</summary>
    public async Task<AccountInfo?> FindAccountAsync(
        Guid accountId, CancellationToken cancellationToken)
    {
        CashRegister? cash = await cashRegisterRepository
            .Where(p => p.Id == accountId).FirstOrDefaultAsync(cancellationToken);

        if (cash is not null)
            return new AccountInfo(cash.Id, cash.Name, AccountKind.CashRegister, cash.CurrencyType.Value);

        Bank? bank = await bankRepository
            .Where(p => p.Id == accountId).FirstOrDefaultAsync(cancellationToken);

        return bank is null
            ? null
            : new AccountInfo(bank.Id, bank.Name, AccountKind.Bank, bank.CurrencyType.Value);
    }

    /// <summary>
    /// Hesaba para girişi ya da çıkışı yazar ve hesabın koşu toplamlarını günceller.
    /// Geriye hareketin kimliği döner; cari tarafı buna bağlanıyor.
    /// </summary>
    public async Task<Guid> PostToAccountAsync(
        AccountInfo account,
        DateOnly date,
        string description,
        decimal deposit,
        decimal withdrawal,
        Guid? contactId,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (account.Kind == AccountKind.CashRegister)
        {
            CashRegister register = (await cashRegisterRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == account.Id, cancellationToken))!;

            register.DepositAmount += deposit;
            register.WithdrawalAmount += withdrawal;

            CashRegisterDetail detail = new()
            {
                CashRegisterId = account.Id,
                Date = date,
                Description = description,
                DepositAmount = deposit,
                WithdrawalAmount = withdrawal,
                ContactId = contactId,
                CategoryId = categoryId
            };

            await cashRegisterDetailRepository.AddAsync(detail, cancellationToken);
            InvalidateAccountCache(AccountKind.CashRegister);
            return detail.Id;
        }

        Bank bank = (await bankRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == account.Id, cancellationToken))!;

        bank.DepositAmount += deposit;
        bank.WithdrawalAmount += withdrawal;

        BankDetail bankDetail = new()
        {
            BankId = account.Id,
            Date = date,
            Description = description,
            DepositAmount = deposit,
            WithdrawalAmount = withdrawal,
            ContactId = contactId,
            CategoryId = categoryId
        };

        await bankDetailRepository.AddAsync(bankDetail, cancellationToken);
        InvalidateAccountCache(AccountKind.Bank);
        return bankDetail.Id;
    }

    /// <summary>Yazılmış bir hesap hareketini geri alır: satır silinir, toplamlar düşer.</summary>
    public async Task RemoveAccountEntryAsync(
        AccountKind kind, Guid entryId, CancellationToken cancellationToken)
    {
        if (kind == AccountKind.CashRegister)
        {
            CashRegisterDetail? detail = await cashRegisterDetailRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == entryId, cancellationToken);

            if (detail is null) return;

            CashRegister? register = await cashRegisterRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == detail.CashRegisterId, cancellationToken);

            if (register is not null)
            {
                register.DepositAmount -= detail.DepositAmount;
                register.WithdrawalAmount -= detail.WithdrawalAmount;
            }

            cashRegisterDetailRepository.Delete(detail);
            InvalidateAccountCache(AccountKind.CashRegister);
            return;
        }

        BankDetail? bankDetail = await bankDetailRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == entryId, cancellationToken);

        if (bankDetail is null) return;

        Bank? bank = await bankRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == bankDetail.BankId, cancellationToken);

        if (bank is not null)
        {
            bank.DepositAmount -= bankDetail.DepositAmount;
            bank.WithdrawalAmount -= bankDetail.WithdrawalAmount;
        }

        bankDetailRepository.Delete(bankDetail);
        InvalidateAccountCache(AccountKind.Bank);
    }

    // --- cari tarafı --------------------------------------------------------

    /// <summary>
    /// Cariye bir ekstre satırı yazar ve bakiyeyi günceller. Cari izlenen hâlde
    /// gelmeli; çağıran zaten onu bulmuş oluyor.
    /// </summary>
    public async Task<ContactTransaction> PostToContactAsync(
        Contact contact,
        DateOnly date,
        string description,
        ContactTransactionKind kind,
        decimal debit,
        decimal credit,
        Guid? invoiceId,
        AccountInfo? account,
        Guid? accountTransactionId,
        CancellationToken cancellationToken)
    {
        contact.DebitAmount += debit;
        contact.CreditAmount += credit;

        ContactTransaction transaction = new()
        {
            ContactId = contact.Id,
            Date = date,
            Description = description,
            Kind = kind,
            DebitAmount = debit,
            CreditAmount = credit,
            InvoiceId = invoiceId,
            AccountKind = account?.Kind,
            AccountId = account?.Id,
            AccountTransactionId = accountTransactionId
        };

        await contactTransactionRepository.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    /// <summary>
    /// Bir cari hareketini ve varsa karşısındaki hesap hareketini birlikte geri
    /// alır. Tahsilatı silmek kasadan da düşmeli; ayrı çağrılara bırakılırsa
    /// biri unutulur.
    /// </summary>
    public async Task RemoveContactEntryAsync(
        ContactTransaction transaction, CancellationToken cancellationToken)
    {
        Contact? contact = await contactRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == transaction.ContactId, cancellationToken);

        if (contact is not null)
        {
            contact.DebitAmount -= transaction.DebitAmount;
            contact.CreditAmount -= transaction.CreditAmount;
        }

        if (transaction is { AccountKind: { } kind, AccountTransactionId: { } entryId })
            await RemoveAccountEntryAsync(kind, entryId, cancellationToken);

        contactTransactionRepository.Delete(transaction);
    }

    /// <summary>Bir faturaya bağlı bütün cari hareketlerini geri alır.</summary>
    public async Task RemoveInvoiceEntriesAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        List<ContactTransaction> transactions = await contactTransactionRepository
            .Where(p => p.InvoiceId == invoiceId)
            .ToListAsync(cancellationToken);

        foreach (ContactTransaction transaction in transactions)
        {
            ContactTransaction? tracked = await contactTransactionRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == transaction.Id, cancellationToken);

            if (tracked is not null)
                await RemoveContactEntryAsync(tracked, cancellationToken);
        }
    }
}
