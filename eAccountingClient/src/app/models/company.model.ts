export class CompanyModel {
  id: string = '';
  name: string = '';
  address: string = '';
  isDeleted: boolean = false;
  taxDepartment: string = '';
  taxNumber: string = '';
  database: DatabaseModel = new DatabaseModel();
}

/**
 * Token'daki firma listesi. Sunucu bunu PascalCase üretiyor, API yanıtları ise
 * camelCase; ikisi tek sınıfta toplanınca firma kaydederken boş "Name" alanı
 * dolu "name" alanını eziyor ve firma adı sunucuya boş gidiyordu.
 */
export class TokenCompanyModel {
  Id: string = '';
  Name: string = '';
}

export class DatabaseModel {
  server: string = '';
  databaseName: string = '';
  username: string = '';
  password: string = '';
}
