"use client";
import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import Header from '../components/Header';
import SpotList from '../components/SpotList';
import { SmokingSpotWithDistance } from '../../types';
import { useFavorites } from '@/contexts/FavoritesContext';

const FavoritesPage: React.FC = () => {
  const { favorites, clearFavorites } = useFavorites();
  const [userLocation, setUserLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [selectedSpot, setSelectedSpot] = useState<SmokingSpotWithDistance | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    // 位置情報を取得
    if (navigator.geolocation) {
      setIsLoading(true);
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setUserLocation({
            lat: position.coords.latitude,
            lng: position.coords.longitude,
          });
          setIsLoading(false);
        },
        (error) => {
          console.error('位置情報の取得に失敗しました:', error);
          setIsLoading(false);
        },
        {
          enableHighAccuracy: false,
          timeout: 10000,
          maximumAge: 300000, // 5分
        }
      );
    }
  }, []);

  const handleSpotSelect = (spot: SmokingSpotWithDistance) => {
    setSelectedSpot(spot);
  };

  const handleClearFavorites = () => {
    if (confirm('すべてのお気に入りを削除しますか？')) {
      clearFavorites();
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-100 dark:from-slate-900 dark:via-slate-800 dark:to-slate-900">
      <Header />
      
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-4">
            ❤️ お気に入りスポット
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            お気に入りに登録したスポットを管理できます
          </p>
        </div>

        {favorites.length === 0 ? (
          <div className="text-center py-12">
            <div className="w-24 h-24 mx-auto mb-6 bg-gray-100 dark:bg-gray-800 rounded-full flex items-center justify-center">
              <svg className="w-12 h-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
              </svg>
            </div>
            <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
              お気に入りがありません
            </h3>
            <p className="text-gray-500 dark:text-gray-400 mb-6">
              スポットをお気に入りに追加すると、ここに表示されます
            </p>
            <Link
              href="/"
              className="inline-flex items-center px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
            >
              <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              スポットを探す
            </Link>
          </div>
        ) : (
          <div className="space-y-6">
            {/* 統計情報 */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="card p-6">
                <div className="flex items-center">
                  <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                    <svg className="w-6 h-6 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                    </svg>
                  </div>
                  <div className="ml-4">
                    <p className="text-sm font-medium text-gray-600 dark:text-gray-400">お気に入り数</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-gray-100">{favorites.length}</p>
                  </div>
                </div>
              </div>

              <div className="card p-6">
                <div className="flex items-center">
                  <div className="p-2 bg-green-100 dark:bg-green-900/30 rounded-lg">
                    <svg className="w-6 h-6 text-green-600 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                  </div>
                  <div className="ml-4">
                    <p className="text-sm font-medium text-gray-600 dark:text-gray-400">カテゴリ数</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                      {new Set(favorites.map(f => f.category)).size}
                    </p>
                  </div>
                </div>
              </div>

              <div className="card p-6">
                <div className="flex items-center">
                  <div className="p-2 bg-purple-100 dark:bg-purple-900/30 rounded-lg">
                    <svg className="w-6 h-6 text-purple-600 dark:text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </div>
                  <div className="ml-4">
                    <p className="text-sm font-medium text-gray-600 dark:text-gray-400">最近追加</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                      {favorites.length > 0 ? 
                        new Date(favorites[favorites.length - 1]?.createdAt || new Date()).toLocaleDateString('ja-JP') : 
                        'なし'
                      }
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* アクションボタン */}
            <div className="flex justify-between items-center">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                お気に入りスポット ({favorites.length}件)
              </h2>
              <button
                onClick={handleClearFavorites}
                className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors text-sm"
              >
                すべて削除
              </button>
            </div>

            {/* スポットリスト */}
            <SpotList
              spots={favorites}
              onSpotSelect={handleSpotSelect}
              selectedSpot={selectedSpot}
              userLocation={userLocation}
            />
          </div>
        )}
      </main>
    </div>
  );
};

export default FavoritesPage; 