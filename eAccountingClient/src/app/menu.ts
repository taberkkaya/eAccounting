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
    icon: 'fas fa-home',
    url: '/',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
  {
    name: 'Yönetim',
    icon: '',
    url: '',
    isTitle: true,
    subMenus: [],
    showThisMenuJustAdmin: true,
  },
  {
    name: 'Kullanıcılar',
    icon: 'fas fa-users',
    url: '/users',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: true,
  },
  {
    name: 'Firmalar',
    icon: 'fas fa-city',
    url: '/companies',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: true,
  },
  {
    name: 'Demo Kayıtları',
    icon: 'fas fa-envelope-open-text',
    url: '/demo-visitors',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: true,
  },
  {
    name: 'Kayıtlar',
    icon: '',
    url: '',
    isTitle: true,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
  {
    name: 'Kasalar',
    icon: 'fas fa-cash-register',
    url: '/cash-registers',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
  {
    name: 'Bankalar',
    icon: 'fas fa-university',
    url: '/banks',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
];
