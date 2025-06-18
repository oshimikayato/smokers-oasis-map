"use client";
import React from 'react';

interface SearchFiltersProps {
  search: string;
  setSearch: (value: string) => void;
  categoryFilter: string;
  setCategoryFilter: (value: string) => void;
  tagFilters: string[];
  setTagFilters: (value: string[]) => void;
  sortBy: "name" | "distance";
  setSortBy: (value: "name" | "distance") => void;
  resetFilters: () => void;
  filteredCount: number;
  totalCount: number;
  userLocation: { lat: number; lng: number } | null;
}

const CATEGORY_OPTIONS = ["喫煙所", "飲食店"];
const TAG_OPTIONS = [
  { value: "屋内", icon: "🏠", color: "blue" },
  { value: "屋外", icon: "🌳", color: "green" },
  { value: "全席喫煙可", icon: "🚬", color: "red" },
  { value: "分煙", icon: "🚭", color: "yellow" },
  { value: "無料", icon: "💰", color: "green" },
  { value: "有料", icon: "💳", color: "purple" },
  { value: "電源あり", icon: "🔌", color: "blue" },
  { value: "Wi-Fiあり", icon: "📶", color: "cyan" }
];

const SearchFilters: React.FC<SearchFiltersProps> = ({
  search,
  setSearch,
  categoryFilter,
  setCategoryFilter,
  tagFilters,
  setTagFilters,
  sortBy,
  setSortBy,
  resetFilters,
  filteredCount,
  totalCount,
  userLocation
}) => {
  const handleTagToggle = (tag: string) => {
    setTagFilters((prev: string[]) => 
      prev.includes(tag) 
        ? prev.filter((t: string) => t !== tag)
        : [...prev, tag]
    );
  };

  return (
    <div className="card p-6 mb-6 animate-fade-in">
      {/* 検索バー */}
      <div className="relative mb-6">
        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
          <svg className="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        <input
          type="text"
          placeholder="キーワード検索（名称・住所・説明）"
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="input pl-10 pr-12"
        />
        {search && (
          <button
            onClick={() => setSearch("")}
            className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600"
          >
            <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>

      {/* フィルターオプション */}
      <div className="space-y-4">
        {/* カテゴリとソート */}
        <div className="flex flex-wrap gap-3">
          <select 
            value={categoryFilter} 
            onChange={e => setCategoryFilter(e.target.value)} 
            className="input max-w-xs"
          >
            <option value="">📂 カテゴリ（全て）</option>
            {CATEGORY_OPTIONS.map(opt => (
              <option key={opt} value={opt}>
                {opt === "喫煙所" ? "🚬 " : "🍽️ "}{opt}
              </option>
            ))}
          </select>
          
          <select 
            value={sortBy} 
            onChange={e => setSortBy(e.target.value as "name" | "distance")} 
            className="input max-w-xs"
          >
            <option value="name">📝 名前順</option>
            <option value="distance" disabled={!userLocation}>
              📍 距離順 {!userLocation && "(位置情報が必要)"}
            </option>
          </select>

          <button
            onClick={resetFilters}
            className="btn btn-ghost"
          >
            <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            リセット
          </button>
        </div>

        {/* タグフィルター */}
        <div>
          <h3 className="text-sm font-medium text-gray-700 mb-3">🏷️ タグで絞り込み</h3>
          <div className="flex flex-wrap gap-2">
            {TAG_OPTIONS.map(tag => (
              <button
                key={tag.value}
                onClick={() => handleTagToggle(tag.value)}
                className={`badge transition-all duration-200 ${
                  tagFilters.includes(tag.value)
                    ? `badge-${tag.color} ring-2 ring-${tag.color}-300`
                    : 'badge-gray hover:bg-gray-200'
                }`}
              >
                <span className="mr-1">{tag.icon}</span>
                {tag.value}
              </button>
            ))}
          </div>
        </div>

        {/* 検索結果表示 */}
        <div className="flex items-center justify-between pt-4 border-t border-gray-200">
          <div className="text-sm text-gray-600">
            <span className="font-medium text-blue-600">{filteredCount}</span> 件の結果
            {filteredCount !== totalCount && (
              <span className="ml-2">（全{totalCount}件中）</span>
            )}
          </div>
          
          <div className="flex items-center space-x-2 text-sm text-gray-500">
            {sortBy === "distance" && userLocation && (
              <div className="flex items-center">
                <svg className="w-4 h-4 mr-1 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                現在地から近い順
              </div>
            )}
            
            {!userLocation && (
              <div className="flex items-center text-yellow-600">
                <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
                </svg>
                位置情報を許可すると距離順ソートが利用できます
              </div>
            )}
          </div>
        </div>

        {/* アクティブフィルター表示 */}
        {(categoryFilter || tagFilters.length > 0 || search) && (
          <div className="pt-3 border-t border-gray-200">
            <div className="flex items-center space-x-2">
              <span className="text-sm font-medium text-gray-700">アクティブフィルター:</span>
              <div className="flex flex-wrap gap-2">
                {categoryFilter && (
                  <span className="badge badge-primary">
                    📂 {categoryFilter}
                    <button
                      onClick={() => setCategoryFilter("")}
                      className="ml-1 hover:text-red-600"
                    >
                      ×
                    </button>
                  </span>
                )}
                {tagFilters.map(tag => (
                  <span key={tag} className="badge badge-secondary">
                    {TAG_OPTIONS.find(t => t.value === tag)?.icon} {tag}
                    <button
                      onClick={() => handleTagToggle(tag)}
                      className="ml-1 hover:text-red-600"
                    >
                      ×
                    </button>
                  </span>
                ))}
                {search && (
                  <span className="badge badge-accent">
                    🔍 "{search}"
                    <button
                      onClick={() => setSearch("")}
                      className="ml-1 hover:text-red-600"
                    >
                      ×
                    </button>
                  </span>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default SearchFilters; 