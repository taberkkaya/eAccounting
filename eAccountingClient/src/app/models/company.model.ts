export class CompanyModel {
  id: string = '';
  name: string = '';
  address: string = '';
  isDeleted: boolean = false;
  taxDepartment: string = '';
  taxNumber: string = '';
  database: DatabaseModel = new DatabaseModel();

  Id: string = '';
  Name: string = '';
}

export class DatabaseModel {
  server: string = '';
  databaseName: string = '';
  username: string = '';
  password: string = '';
}
