"use client";
import React, { createContext, useContext, useEffect, useState } from 'react';
import { SmokingSpotWithDistance } from '@/types';

interface HistoryContextType {
  history: SmokingSpotWithDistance[];
  addToHistory: (spot: SmokingSpotWithDistance) => void;
  clearHistory: () => void;
  removeFromHistory: (spotId: number) => void;
  getRecentSpots: (limit?: number) => SmokingSpotWithDistance[];
}

const HistoryContext = createContext<HistoryContextType | undefined>(undefined);

export function HistoryProvider({ children }: { children: React.ReactNode }) {
  const [history, setHistory] = useState<SmokingSpotWithDistance[]>([]);

  useEffect(() => {
    // ローカルストレージから履歴を読み込み
    const savedHistory = localStorage.getItem('spotHistory');
    if (savedHistory) {
      try {
        const parsedHistory = JSON.parse(savedHistory);
        // 日付を正しく復元
        const historyWithDates = parsedHistory.map((item: any) => ({
          ...item,
          createdAt: new Date(item.createdAt),
          updatedAt: new Date(item.updatedAt)
        }));
        setHistory(historyWithDates);
      } catch (error) {
        console.error('履歴の読み込みに失敗しました:', error);
      }
    }
  }, []);

  useEffect(() => {
    // 履歴が変更されたらローカルストレージに保存
    localStorage.setItem('spotHistory', JSON.stringify(history));
  }, [history]);

  const addToHistory = (spot: SmokingSpotWithDistance) => {
    setHistory(prev => {
      // 既存の同じスポットを削除
      const filtered = prev.filter(item => item.id !== spot.id);
      // 新しいスポットを先頭に追加
      return [spot, ...filtered].slice(0, 50); // 最大50件まで保持
    });
  };

  const clearHistory = () => {
    setHistory([]);
  };

  const removeFromHistory = (spotId: number) => {
    setHistory(prev => prev.filter(item => item.id !== spotId));
  };

  const getRecentSpots = (limit: number = 10) => {
    return history.slice(0, limit);
  };

  return (
    <HistoryContext.Provider value={{
      history,
      addToHistory,
      clearHistory,
      removeFromHistory,
      getRecentSpots
    }}>
      {children}
    </HistoryContext.Provider>
  );
}

export function useHistory() {
  const context = useContext(HistoryContext);
  if (context === undefined) {
    throw new Error('useHistory must be used within a HistoryProvider');
  }
  return context;
} 