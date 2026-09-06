export class CategoryModel {
  id: string = '';
  name: string = '';
  /** 0 gelir, 1 gider — hareketin yönüyle aynı. */
  direction: number = 0;
  /** Bu kalemle etiketlenmiş hareket sayısı. */
  usageCount: number = 0;
}
