import { Area } from './area';

export interface Company {
  id: string;
  name: string;
  address: {
    street: string;
    number: number;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  };
  website: string;
  base64Logo?: string;
  areas: Area[];
}
