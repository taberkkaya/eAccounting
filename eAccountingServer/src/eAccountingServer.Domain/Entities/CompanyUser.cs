using eAccountingServer.Domain.Abstractions;
using eAccountingServer.Domain.Users;

namespace eAccountingServer.Domain.Entities;
public sealed class CompanyUser : Entity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid AppUserId { get; set; }
}
