"use client";
import React, { useState } from 'react';
import { SearchOperator, SearchCondition, SearchGroup, AdvancedSearchConfig } from '@/types';

interface AdvancedSearchProps {
  config: AdvancedSearchConfig;
  onConfigChange: (config: AdvancedSearchConfig) => void;
  onClose: () => void;
}

const FIELD_OPTIONS = [
  { value: 'name', label: '名称', icon: '📝' },
  { value: 'address', label: '住所', icon: '📍' },
  { value: 'description', label: '説明', icon: '📄' },
  { value: 'category', label: 'カテゴリ', icon: '📂' },
  { value: 'tags', label: 'タグ', icon: '🏷️' }
];

const OPERATOR_OPTIONS = [
  { value: 'contains', label: '含む', icon: '🔍' },
  { value: 'equals', label: '完全一致', icon: '✅' },
  { value: 'startsWith', label: '始まる', icon: '🚀' },
  { value: 'endsWith', label: '終わる', icon: '🏁' }
];

const AdvancedSearch: React.FC<AdvancedSearchProps> = ({
  config,
  onConfigChange,
  onClose
}) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const addGroup = () => {
    const newGroup: SearchGroup = {
      id: `group-${Date.now()}`,
      conditions: [{
        field: 'name',
        operator: 'contains',
        value: ''
      }],
      operator: 'AND'
    };

    onConfigChange({
      ...config,
      groups: [...config.groups, newGroup]
    });
  };

  const removeGroup = (groupId: string) => {
    onConfigChange({
      ...config,
      groups: config.groups.filter(g => g.id !== groupId)
    });
  };

  const updateGroupOperator = (groupId: string, operator: SearchOperator) => {
    onConfigChange({
      ...config,
      groups: config.groups.map(g => 
        g.id === groupId ? { ...g, operator } : g
      )
    });
  };

  const addCondition = (groupId: string) => {
    onConfigChange({
      ...config,
      groups: config.groups.map(g => 
        g.id === groupId ? {
          ...g,
          conditions: [...g.conditions, {
            field: 'name',
            operator: 'contains',
            value: ''
          }]
        } : g
      )
    });
  };

  const removeCondition = (groupId: string, conditionIndex: number) => {
    onConfigChange({
      ...config,
      groups: config.groups.map(g => 
        g.id === groupId ? {
          ...g,
          conditions: g.conditions.filter((_, index) => index !== conditionIndex)
        } : g
      )
    });
  };

  const updateCondition = (groupId: string, conditionIndex: number, updates: Partial<SearchCondition>) => {
    onConfigChange({
      ...config,
      groups: config.groups.map(g => 
        g.id === groupId ? {
          ...g,
          conditions: g.conditions.map((c, index) => 
            index === conditionIndex ? { ...c, ...updates } : c
          )
        } : g
      )
    });
  };

  const updateGlobalGroupOperator = (operator: SearchOperator) => {
    onConfigChange({
      ...config,
      groupOperator: operator
    });
  };

  const resetSearch = () => {
    onConfigChange({
      groups: [],
      groupOperator: 'AND'
    });
  };

  const hasActiveSearch = config.groups.some(g => 
    g.conditions.some(c => c.value.trim() !== '')
  );

  return (
    <div className="bg-white/90 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 p-6">
      {/* ヘッダー */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <span className="text-2xl">🔍</span>
          <h3 className="text-xl font-bold text-gray-800">高度な検索</h3>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="p-2 text-gray-600 hover:text-gray-800 hover:bg-gray-100 rounded-xl transition-all duration-200"
            aria-label={isExpanded ? "折りたたむ" : "展開する"}
          >
            <svg className={`w-5 h-5 transform transition-transform duration-200 ${isExpanded ? 'rotate-180' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
          <button
            onClick={onClose}
            className="p-2 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded-xl transition-all duration-200"
            aria-label="閉じる"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      {/* グループ間の演算子 */}
      {config.groups.length > 1 && (
        <div className="mb-6 p-4 bg-blue-50 rounded-2xl border border-blue-200">
          <div className="flex items-center gap-3">
            <span className="text-sm font-semibold text-blue-800">グループ間の条件:</span>
            <div className="flex gap-2">
              <button
                onClick={() => updateGlobalGroupOperator('AND')}
                className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all duration-200 ${
                  config.groupOperator === 'AND'
                    ? 'bg-blue-600 text-white shadow-lg'
                    : 'bg-white text-blue-600 border-2 border-blue-200 hover:bg-blue-50'
                }`}
              >
                AND（全てのグループに一致）
              </button>
              <button
                onClick={() => updateGlobalGroupOperator('OR')}
                className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all duration-200 ${
                  config.groupOperator === 'OR'
                    ? 'bg-blue-600 text-white shadow-lg'
                    : 'bg-white text-blue-600 border-2 border-blue-200 hover:bg-blue-50'
                }`}
              >
                OR（いずれかのグループに一致）
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 検索グループ */}
      <div className="space-y-4">
        {config.groups.map((group, groupIndex) => (
          <div key={group.id} className="bg-gray-50 rounded-2xl p-4 border border-gray-200">
            {/* グループヘッダー */}
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-3">
                <span className="text-lg font-semibold text-gray-700">
                  グループ {groupIndex + 1}
                </span>
                {group.conditions.length > 1 && (
                  <div className="flex gap-2">
                    <button
                      onClick={() => updateGroupOperator(group.id, 'AND')}
                      className={`px-3 py-1 rounded-lg text-xs font-semibold transition-all duration-200 ${
                        group.operator === 'AND'
                          ? 'bg-green-600 text-white'
                          : 'bg-white text-green-600 border border-green-200 hover:bg-green-50'
                      }`}
                    >
                      AND
                    </button>
                    <button
                      onClick={() => updateGroupOperator(group.id, 'OR')}
                      className={`px-3 py-1 rounded-lg text-xs font-semibold transition-all duration-200 ${
                        group.operator === 'OR'
                          ? 'bg-green-600 text-white'
                          : 'bg-white text-green-600 border border-green-200 hover:bg-green-50'
                      }`}
                    >
                      OR
                    </button>
                  </div>
                )}
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => addCondition(group.id)}
                  className="p-2 text-green-600 hover:text-green-700 hover:bg-green-50 rounded-lg transition-all duration-200"
                  aria-label="条件を追加"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                  </svg>
                </button>
                {config.groups.length > 1 && (
                  <button
                    onClick={() => removeGroup(group.id)}
                    className="p-2 text-red-600 hover:text-red-700 hover:bg-red-50 rounded-lg transition-all duration-200"
                    aria-label="グループを削除"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                )}
              </div>
            </div>

            {/* 条件リスト */}
            <div className="space-y-3">
              {group.conditions.map((condition, conditionIndex) => (
                <div key={conditionIndex} className="flex items-center gap-3 p-3 bg-white rounded-xl border border-gray-200">
                  <select
                    value={condition.field}
                    onChange={(e) => updateCondition(group.id, conditionIndex, { field: e.target.value as any })}
                    className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm"
                  >
                    {FIELD_OPTIONS.map(option => (
                      <option key={option.value} value={option.value}>
                        {option.icon} {option.label}
                      </option>
                    ))}
                  </select>

                  <select
                    value={condition.operator}
                    onChange={(e) => updateCondition(group.id, conditionIndex, { operator: e.target.value as any })}
                    className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm"
                  >
                    {OPERATOR_OPTIONS.map(option => (
                      <option key={option.value} value={option.value}>
                        {option.icon} {option.label}
                      </option>
                    ))}
                  </select>

                  <input
                    type="text"
                    value={condition.value}
                    onChange={(e) => updateCondition(group.id, conditionIndex, { value: e.target.value })}
                    placeholder="検索値を入力..."
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm"
                  />

                  {group.conditions.length > 1 && (
                    <button
                      onClick={() => removeCondition(group.id, conditionIndex)}
                      className="p-2 text-red-600 hover:text-red-700 hover:bg-red-50 rounded-lg transition-all duration-200"
                      aria-label="条件を削除"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  )}
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      {/* アクションボタン */}
      <div className="flex items-center justify-between mt-6 pt-6 border-t border-gray-200">
        <div className="flex gap-3">
          <button
            onClick={addGroup}
            className="px-4 py-2 bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-all duration-200 flex items-center gap-2 font-semibold"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            グループを追加
          </button>
          {hasActiveSearch && (
            <button
              onClick={resetSearch}
              className="px-4 py-2 bg-gray-600 text-white rounded-xl hover:bg-gray-700 transition-all duration-200 flex items-center gap-2 font-semibold"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              リセット
            </button>
          )}
        </div>

        <div className="text-sm text-gray-600">
          {hasActiveSearch ? (
            <span className="flex items-center gap-2">
              <span className="w-2 h-2 bg-green-500 rounded-full"></span>
              検索条件が設定されています
            </span>
          ) : (
            <span className="flex items-center gap-2">
              <span className="w-2 h-2 bg-gray-400 rounded-full"></span>
              検索条件を設定してください
            </span>
          )}
        </div>
      </div>
    </div>
  );
};

export default AdvancedSearch; 