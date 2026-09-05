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
    name: 'Hareketler',
    icon: 'fas fa-exchange-alt',
    url: '/movements',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
  {
    name: 'Hesaplar',
    icon: 'fas fa-wallet',
    url: '/accounts',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
  {
    name: 'Gelir/Gider Kalemleri',
    icon: 'fas fa-tags',
    url: '/categories',
    isTitle: false,
    subMenus: [],
    showThisMenuJustAdmin: false,
  },
];
