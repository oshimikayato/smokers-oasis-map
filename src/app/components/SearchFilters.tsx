"use client";
import React, { useState, useEffect } from 'react';

interface SearchFiltersProps {
  search: string;
  setSearch: (value: string) => void;
  categoryFilter: string;
  setCategoryFilter: (value: string) => void;
  tagFilters: string[];
  setTagFilters: (value: string[] | ((prev: string[]) => string[])) => void;
  sortBy: "name" | "distance";
  setSortBy: (value: "name" | "distance") => void;
  resetFilters: () => void;
  filteredCount?: number;
  totalCount?: number;
  userLocation?: { lat: number; lng: number } | null;
}

const CATEGORY_OPTIONS = [
  { value: "", label: "📂 カテゴリ（全て）" },
  { value: "喫煙所", label: "🚬 喫煙所" },
  { value: "飲食店", label: "🍽️ 飲食店" }
];

const TAG_OPTIONS = [
  { value: "屋内", icon: "🏠", color: "blue" },
  { value: "屋外", icon: "🌳", color: "green" },
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
  const [isSearchFocused, setIsSearchFocused] = useState(false);
  const [debouncedSearch, setDebouncedSearch] = useState(search);

  // 検索のデバウンス処理
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
    }, 300);

    return () => clearTimeout(timer);
  }, [search]);

  const handleTagToggle = (tag: string) => {
    setTagFilters((prev: string[]) => 
      prev.includes(tag) 
        ? prev.filter((t: string) => t !== tag)
        : [...prev, tag]
    );
  };

  const hasActiveFilters = categoryFilter || tagFilters.length > 0 || search;

  return (
    <div className="bg-white rounded-lg shadow-md p-6 space-y-6">
      {/* 検索バー */}
      <div className="relative">
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
          onFocus={() => setIsSearchFocused(true)}
          onBlur={() => setIsSearchFocused(false)}
          className={`w-full pl-10 pr-12 py-3 border rounded-lg transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent ${
            isSearchFocused ? 'border-blue-500' : 'border-gray-300'
          }`}
        />
        {search && (
          <button
            onClick={() => setSearch("")}
            className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 transition-colors"
            aria-label="検索をクリア"
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
            className="px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
          >
            {CATEGORY_OPTIONS.map(opt => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
          
          <select 
            value={sortBy} 
            onChange={e => setSortBy(e.target.value as "name" | "distance")} 
            className="px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
          >
            <option value="name">📝 名前順</option>
            <option value="distance" disabled={!userLocation}>
              📍 距離順 {!userLocation && "(位置情報が必要)"}
            </option>
          </select>

          {hasActiveFilters && (
            <button
              onClick={resetFilters}
              className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors flex items-center"
            >
              <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              リセット
            </button>
          )}
        </div>

        {/* タグフィルター */}
        <div>
          <h3 className="text-sm font-medium text-gray-700 mb-3">🏷️ タグで絞り込み</h3>
          <div className="flex flex-wrap gap-2">
            {TAG_OPTIONS.map(tag => (
              <button
                key={tag.value}
                onClick={() => handleTagToggle(tag.value)}
                className={`px-3 py-2 rounded-full text-sm font-medium transition-all duration-200 ${
                  tagFilters.includes(tag.value)
                    ? `bg-${tag.color}-100 text-${tag.color}-800 ring-2 ring-${tag.color}-300`
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                }`}
              >
                <span className="mr-1">{tag.icon}</span>
                {tag.value}
              </button>
            ))}
          </div>
        </div>

        {/* 検索結果表示 */}
        {(filteredCount !== undefined || totalCount !== undefined) && (
          <div className="flex items-center justify-between pt-4 border-t border-gray-200">
            <div className="text-sm text-gray-600">
              <span className="font-medium text-blue-600">{filteredCount || 0}</span> 件の結果
              {filteredCount !== totalCount && totalCount !== undefined && (
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
        )}

        {/* アクティブフィルター表示 */}
        {hasActiveFilters && (
          <div className="pt-3 border-t border-gray-200">
            <div className="flex items-center space-x-2">
              <span className="text-sm font-medium text-gray-700">アクティブフィルター:</span>
              <div className="flex flex-wrap gap-2">
                {categoryFilter && (
                  <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded-full">
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
                  <span key={tag} className="px-2 py-1 bg-gray-100 text-gray-800 text-xs rounded-full">
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
                  <span className="px-2 py-1 bg-green-100 text-green-800 text-xs rounded-full">
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