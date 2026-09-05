import { TokenCompanyModel } from './company.model';
import { CompanyUserModel } from './companyUser.model';

export class UserModel {
  id: string = '';
  name: string = '';
  firstName: string = '';
  lastName: string = '';
  fullName: string = '';
  password: string | null = '';
  userName: string = '';
  email: string = '';
  companyId: string = '';
  companyIds: string[] = [];
  companyUsers: CompanyUserModel[] = [];
  companies: TokenCompanyModel[] = [];
  isAdmin: boolean = false;
}
