export interface PagedResult<TItem> {
  items: TItem[];
  total: number;
  page: number;
  pageSize: number;
}
