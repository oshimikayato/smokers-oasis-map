"use client";
import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import SpotList from '../components/SpotList';
import { SmokingSpotWithDistance, UserLocation } from "../../types";

const FavoritesPage: React.FC = () => {
  const [favorites, setFavorites] = useState<number[]>([]);
  const [favoriteSpots, setFavoriteSpots] = useState<SmokingSpotWithDistance[]>([]);
  const [allSpots, setAllSpots] = useState<SmokingSpotWithDistance[]>([]);
  const [userLocation, setUserLocation] = useState<UserLocation | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // お気に入りスポットを取得
  useEffect(() => {
    if (typeof window !== "undefined") {
      const fav = localStorage.getItem("favorites");
      const favoritesData = fav ? JSON.parse(fav) : [];
      setFavorites(favoritesData);
    }
  }, []);

  // 現在地取得
  useEffect(() => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          const location = { lat: pos.coords.latitude, lng: pos.coords.longitude };
          setUserLocation(location);
        },
        () => {
          console.log("位置情報の取得に失敗しました");
        }
      );
    }
  }, []);

  // 全スポットデータを取得
  useEffect(() => {
    setIsLoading(true);
    fetch("/api/spots")
      .then((res) => res.json())
      .then((data) => {
        setAllSpots(data);
        setIsLoading(false);
      })
      .catch((error) => {
        console.error("データ取得エラー:", error);
        setIsLoading(false);
      });
  }, []);

  // お気に入りスポットをフィルタリング
  useEffect(() => {
    const filtered = allSpots.filter(spot => favorites.includes(spot['id']));
    
    // 距離計算とソート
    if (userLocation) {
      const spotsWithDistance = filtered.map(spot => ({
        ...spot,
        distance: calculateDistance(userLocation.lat, userLocation.lng, spot['lat'], spot['lng'])
      })).sort((a, b) => (a.distance || 0) - (b.distance || 0));
      setFavoriteSpots(spotsWithDistance);
    } else {
      setFavoriteSpots(filtered.sort((a, b) => a['name'].localeCompare(b['name'])));
    }
  }, [allSpots, favorites, userLocation]);

  const toggleFavorite = (spotId: number) => {
    const newFavorites = favorites.includes(spotId) 
      ? favorites.filter(id => id !== spotId) 
      : [...favorites, spotId];
    
    setFavorites(newFavorites);
    if (typeof window !== "undefined") {
      localStorage.setItem("favorites", JSON.stringify(newFavorites));
    }
  };

  const handleSelectSpot = (spot: SmokingSpotWithDistance) => {
    // スポット詳細ページに遷移（将来的に実装）
    console.log("スポット選択:", spot);
  };

  // 距離計算関数
  function calculateDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371; // 地球の半径（km）
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = 
      Math.sin(dLat/2) * Math.sin(dLat/2) +
      Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * 
      Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    return R * c;
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* ヘッダー */}
      <div className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div className="flex items-center gap-4">
              <Link 
                href="/"
                className="text-gray-600 hover:text-gray-900 transition-colors"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
                </svg>
              </Link>
              <h1 className="text-2xl font-bold text-gray-900">お気に入りスポット</h1>
            </div>
            <div className="flex items-center gap-2">
              <svg className="w-6 h-6 text-red-500" fill="currentColor" viewBox="0 0 24 24">
                <path d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
              </svg>
              <span className="text-sm text-gray-600">{favorites.length}件</span>
            </div>
          </div>
        </div>
      </div>

      {/* メインコンテンツ */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {favorites.length === 0 ? (
          <div className="text-center py-12">
            <div className="w-24 h-24 mx-auto mb-6 bg-gray-100 rounded-full flex items-center justify-center">
              <svg className="w-12 h-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
              </svg>
            </div>
            <h2 className="text-xl font-semibold text-gray-900 mb-2">
              お気に入りスポットがありません
            </h2>
            <p className="text-gray-600 mb-6">
              マップでスポットをお気に入りに追加すると、ここに表示されます
            </p>
            <Link
              href="/"
              className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-1.447-.894L15 4m0 13V4m-6 3l6-3" />
              </svg>
              マップを見る
            </Link>
          </div>
        ) : (
          <div className="space-y-6">
            {/* 統計情報 */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="bg-white p-4 rounded-lg shadow-sm border">
                <div className="flex items-center">
                  <div className="p-2 bg-red-100 rounded-lg">
                    <svg className="w-6 h-6 text-red-600" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                    </svg>
                  </div>
                  <div className="ml-3">
                    <p className="text-sm font-medium text-gray-600">お気に入り数</p>
                    <p className="text-2xl font-bold text-gray-900">{favoriteSpots.length}</p>
                  </div>
                </div>
              </div>

              <div className="bg-white p-4 rounded-lg shadow-sm border">
                <div className="flex items-center">
                  <div className="p-2 bg-blue-100 rounded-lg">
                    <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                  </div>
                  <div className="ml-3">
                    <p className="text-sm font-medium text-gray-600">カテゴリ</p>
                    <p className="text-2xl font-bold text-gray-900">
                      {new Set(favoriteSpots.map(spot => spot['category'])).size}
                    </p>
                  </div>
                </div>
              </div>

              <div className="bg-white p-4 rounded-lg shadow-sm border">
                <div className="flex items-center">
                  <div className="p-2 bg-green-100 rounded-lg">
                    <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
                    </svg>
                  </div>
                  <div className="ml-3">
                    <p className="text-sm font-medium text-gray-600">平均距離</p>
                    <p className="text-2xl font-bold text-gray-900">
                      {userLocation && favoriteSpots.length > 0
                        ? `${(favoriteSpots.reduce((sum, spot) => sum + (spot.distance || 0), 0) / favoriteSpots.length).toFixed(1)}km`
                        : '-'
                      }
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* スポットリスト */}
            <SpotList
              spots={favoriteSpots}
              favorites={favorites}
              onToggleFavorite={toggleFavorite}
              onSelectSpot={handleSelectSpot}
              userLocation={userLocation}
            />
          </div>
        )}
      </div>
    </div>
  );
};

export default FavoritesPage; 