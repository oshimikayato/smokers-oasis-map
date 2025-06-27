import type { Prisma } from '@prisma/client';

// Prismaから生成される型
export type SmokingSpot = Prisma.SmokingSpotGetPayload<{}>;
export type Feedback = Prisma.FeedbackGetPayload<{}>;
export type Photo = Prisma.PhotoGetPayload<{}>;

// カスタム型定義（nullableフィールドを適切に処理）
export interface SmokingSpotWithDistance extends Omit<SmokingSpot, 'address' | 'description'> {
  address: string | null;
  description: string | null;
  distance?: number;
}

export interface FeedbackForm {
  found: boolean | undefined;
  rating: number;
  comment: string;
  reportType: string;
}

export interface PhotoForm {
  url: string;
  caption: string;
}

export interface UserLocation {
  lat: number;
  lng: number;
}

export type SortOption = "name" | "distance";
export type ViewMode = "map" | "list"; 