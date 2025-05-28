export class MenuModel {
  name: string = '';
  icon: string = '';
  url: string = '';
  isTitle: boolean = false;
  subMenus: MenuModel[] = [];
  showThisMenuJustAdmin: boolean = false;
}

export const Menus: MenuModel[] = [
  {
    name: 'Ana Sayfa',
    icon: 'fa-solid fa-home',
    url: '/',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
  {
    name: 'Admin',
    icon: '',
    url: '',
    isTitle: true,
    subMenus: [],
    showThisMenuJustAdmin: true,
  },
  {
    name: 'Users',
    icon: 'fa-solid fa-users',
    url: '/users',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: true,
  },
  {
    name: 'Companies',
    icon: 'fa-solid fa-city',
    url: '/companies',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: true,
  },
  {
    name: 'Entries',
    icon: '',
    url: '',
    isTitle: true,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
  {
    name: 'Cash Registers',
    icon: 'fa-solid fa-cash-register',
    url: '/cash-registers',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
];
