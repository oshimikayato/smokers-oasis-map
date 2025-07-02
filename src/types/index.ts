// 基本型定義
export interface SmokingSpot {
  id: number;
  name: string;
  lat: number;
  lng: number;
  address: string | null;
  description: string | null;
  category: string;
  tags: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface Feedback {
  id: number;
  spotId: number;
  found: boolean;
  rating: number;
  comment: string;
  reportType: string;
  createdAt: Date;
}

export interface Photo {
  id: number;
  spotId: number;
  url: string;
  caption: string;
  createdAt: Date;
}

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
  accuracy?: number; // 精度（メートル）
  timestamp?: number; // タイムスタンプ
}

// AND/OR検索用の型定義
export type SearchOperator = 'AND' | 'OR';

export interface SearchCondition {
  field: 'name' | 'address' | 'description' | 'category' | 'tags';
  operator: 'contains' | 'equals' | 'startsWith' | 'endsWith';
  value: string;
}

export interface SearchGroup {
  id: string;
  conditions: SearchCondition[];
  operator: SearchOperator;
}

export interface AdvancedSearchConfig {
  groups: SearchGroup[];
  groupOperator: SearchOperator;
}

export type SortOption = "name" | "distance";
export type ViewMode = "map" | "list"; 