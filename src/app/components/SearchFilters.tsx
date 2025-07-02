"use client";
import React, { useState } from 'react';
import { SortOption, UserLocation, AdvancedSearchConfig } from '@/types';
import AdvancedSearch from './AdvancedSearch';

interface SearchFiltersProps {
  search: string;
  setSearch: (search: string) => void;
  categoryFilter: string;
  setCategoryFilter: (category: string) => void;
  tagFilters: string[];
  setTagFilters: (value: string[] | ((prev: string[]) => string[])) => void;
  sortBy: SortOption;
  setSortBy: (value: SortOption) => void;
  resetFilters: () => void;
  filteredCount?: number;
  totalCount?: number;
  userLocation?: UserLocation | null;
  getUserLocation?: () => void;
  locationError?: string | null;
  isLocationLoading?: boolean;
  advancedSearchConfig?: AdvancedSearchConfig;
  onAdvancedSearchChange?: (config: AdvancedSearchConfig) => void;
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
  userLocation,
  getUserLocation,
  locationError,
  isLocationLoading,
  advancedSearchConfig,
  onAdvancedSearchChange
}) => {
  const [isSearchFocused, setIsSearchFocused] = useState(false);
  const [showAdvancedSearch, setShowAdvancedSearch] = useState(false);

  const handleTagToggle = (tag: string) => {
    setTagFilters((prev: string[]) => 
      prev.includes(tag) 
        ? prev.filter((t: string) => t !== tag)
        : [...prev, tag]
    );
  };

  const hasActiveFilters = categoryFilter || tagFilters.length > 0 || search || 
    (advancedSearchConfig && advancedSearchConfig.groups.some(g => 
      g.conditions.some(c => c.value.trim() !== '')
    ));

  const handleAdvancedSearchChange = (config: AdvancedSearchConfig) => {
    if (onAdvancedSearchChange) {
      onAdvancedSearchChange(config);
    }
  };

  return (
    <div className="bg-white/80 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 p-8 space-y-8">
      {/* 高度な検索セクション */}
      {showAdvancedSearch && advancedSearchConfig && onAdvancedSearchChange && (
        <AdvancedSearch
          config={advancedSearchConfig}
          onConfigChange={handleAdvancedSearchChange}
          onClose={() => setShowAdvancedSearch(false)}
        />
      )}

      {/* 検索バー */}
      <div className="relative">
        <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
          <svg className="h-6 w-6 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
          className={`w-full pl-12 pr-12 py-4 border-2 rounded-2xl transition-all duration-300 focus:outline-none focus:ring-4 focus:ring-blue-500/20 text-lg ${
            isSearchFocused ? 'border-blue-500 bg-blue-50/50' : 'border-gray-200 bg-white/50'
          }`}
        />
        {search && (
          <button
            onClick={() => setSearch("")}
            className="absolute inset-y-0 right-0 pr-4 flex items-center text-gray-400 hover:text-gray-600 transition-colors"
            aria-label="検索をクリア"
          >
            <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>

      {/* フィルターオプション */}
      <div className="space-y-6">
        {/* カテゴリとソート */}
        <div className="flex flex-wrap gap-4">
          <select 
            value={categoryFilter} 
            onChange={e => setCategoryFilter(e.target.value)} 
            className="px-6 py-3 border-2 border-gray-200 rounded-2xl focus:outline-none focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 transition-all duration-300 bg-white/50 text-lg font-medium"
          >
            {CATEGORY_OPTIONS.map(opt => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
          
          <select 
            value={sortBy} 
            onChange={e => setSortBy(e.target.value as SortOption)} 
            className="px-6 py-3 border-2 border-gray-200 rounded-2xl focus:outline-none focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 transition-all duration-300 bg-white/50 text-lg font-medium"
          >
            <option value="name">📝 名前順</option>
            <option value="distance" disabled={!userLocation}>
              📍 距離順 {!userLocation && "(位置情報が必要)"}
            </option>
          </select>

          {/* 高度な検索ボタン */}
          {advancedSearchConfig && onAdvancedSearchChange && (
            <button
              onClick={() => setShowAdvancedSearch(!showAdvancedSearch)}
              className={`px-6 py-3 rounded-2xl transition-all duration-300 flex items-center gap-2 font-semibold shadow-lg hover:shadow-xl transform hover:scale-105 ${
                showAdvancedSearch
                  ? 'bg-gradient-to-r from-purple-500 to-purple-600 text-white'
                  : 'bg-gradient-to-r from-blue-500 to-blue-600 text-white hover:from-blue-600 hover:to-blue-700'
              }`}
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {showAdvancedSearch ? '高度な検索を閉じる' : '高度な検索'}
            </button>
          )}

          {hasActiveFilters && (
            <button
              onClick={resetFilters}
              className="px-6 py-3 bg-gradient-to-r from-gray-100 to-gray-200 text-gray-700 rounded-2xl hover:from-gray-200 hover:to-gray-300 transition-all duration-300 flex items-center gap-2 font-semibold shadow-lg hover:shadow-xl transform hover:scale-105"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              リセット
            </button>
          )}
        </div>

        {/* タグフィルター */}
        <div>
          <h3 className="text-lg font-bold text-gray-800 mb-4 flex items-center gap-2">
            <span className="text-2xl">🏷️</span>
            タグで絞り込み
          </h3>
          <div className="flex flex-wrap gap-3">
            {TAG_OPTIONS.map(tag => (
              <button
                key={tag.value}
                onClick={() => handleTagToggle(tag.value)}
                className={`px-4 py-3 rounded-2xl text-sm font-semibold transition-all duration-300 transform hover:scale-105 ${
                  tagFilters.includes(tag.value)
                    ? `bg-gradient-to-r from-${tag.color}-500 to-${tag.color}-600 text-white shadow-lg`
                    : 'bg-white/50 text-gray-700 hover:bg-white border-2 border-gray-200 hover:border-gray-300'
                }`}
              >
                <span className="mr-2">{tag.icon}</span>
                {tag.value}
              </button>
            ))}
          </div>
        </div>

        {/* 検索結果表示 */}
        {(filteredCount !== undefined || totalCount !== undefined) && (
          <div className="flex items-center justify-between pt-6 border-t border-gray-200">
            <div className="text-lg text-gray-700">
              <span className="font-bold text-blue-600 text-xl">{filteredCount || 0}</span> 件の結果
              {filteredCount !== totalCount && totalCount !== undefined && (
                <span className="ml-2 text-gray-500">（全{totalCount}件中）</span>
              )}
            </div>
            
            <div className="flex items-center space-x-3 text-sm text-gray-500">
              {sortBy === "distance" && userLocation && (
                <div className="flex items-center bg-green-50 px-3 py-2 rounded-xl border border-green-200">
                  <svg className="w-4 h-4 mr-2 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  現在地から近い順
                </div>
              )}
              
              {!userLocation && (
                <div className="flex items-center bg-yellow-50 px-3 py-2 rounded-xl border border-yellow-200">
                  <svg className="w-4 h-4 mr-2 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
          <div className="pt-6 border-t border-gray-200">
            <div className="flex items-center space-x-3">
              <span className="text-lg font-bold text-gray-800">アクティブフィルター:</span>
              <div className="flex flex-wrap gap-3">
                {categoryFilter && (
                  <span className="px-4 py-2 bg-gradient-to-r from-blue-500 to-blue-600 text-white text-sm font-semibold rounded-xl shadow-lg">
                    📂 {categoryFilter}
                    <button
                      onClick={() => setCategoryFilter("")}
                      className="ml-2 hover:text-red-200 transition-colors"
                    >
                      ×
                    </button>
                  </span>
                )}
                {tagFilters.map(tag => (
                  <span key={tag} className="px-4 py-2 bg-gradient-to-r from-purple-500 to-purple-600 text-white text-sm font-semibold rounded-xl shadow-lg">
                    {TAG_OPTIONS.find(t => t.value === tag)?.icon} {tag}
                    <button
                      onClick={() => handleTagToggle(tag)}
                      className="ml-2 hover:text-red-200 transition-colors"
                    >
                      ×
                    </button>
                  </span>
                ))}
                {search && (
                  <span className="px-4 py-2 bg-gradient-to-r from-green-500 to-green-600 text-white text-sm font-semibold rounded-xl shadow-lg">
                    🔍 "{search}"
                    <button
                      onClick={() => setSearch("")}
                      className="ml-2 hover:text-red-200 transition-colors"
                    >
                      ×
                    </button>
                  </span>
                )}
                {advancedSearchConfig && advancedSearchConfig.groups.some(g => 
                  g.conditions.some(c => c.value.trim() !== '')
                ) && (
                  <span className="px-4 py-2 bg-gradient-to-r from-orange-500 to-orange-600 text-white text-sm font-semibold rounded-xl shadow-lg">
                    ⚙️ 高度な検索
                    <button
                      onClick={() => setShowAdvancedSearch(true)}
                      className="ml-2 hover:text-red-200 transition-colors"
                    >
                      ✏️
                    </button>
                  </span>
                )}
              </div>
            </div>
          </div>
        )}

        {/* 位置情報セクション */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            📍 現在地
          </label>
          <div className="flex items-center gap-3">
            {userLocation ? (
              <div className="flex-1 p-3 bg-green-50 border border-green-200 rounded-2xl">
                <p className="text-sm text-green-800">
                  取得済み: {userLocation.lat.toFixed(6)}, {userLocation.lng.toFixed(6)}
                </p>
                {userLocation.accuracy && (
                  <p className="text-xs text-green-600 mt-1">
                    精度: ±{Math.round(userLocation.accuracy)}m
                  </p>
                )}
              </div>
            ) : (
              <div className="flex-1 p-3 bg-gray-50 border border-gray-200 rounded-2xl">
                <p className="text-sm text-gray-600">
                  位置情報を取得中...
                </p>
              </div>
            )}
            {getUserLocation && (
              <button
                onClick={getUserLocation}
                disabled={isLocationLoading}
                className="px-4 py-2 bg-blue-500 text-white rounded-xl hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200"
              >
                {isLocationLoading ? '取得中...' : '再取得'}
              </button>
            )}
          </div>
          {locationError && (
            <p className="text-sm text-red-600 mt-2">{locationError}</p>
          )}
        </div>
      </div>
    </div>
  );
};

export default SearchFilters; 