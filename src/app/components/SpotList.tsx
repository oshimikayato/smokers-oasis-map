"use client";
import React, { useState } from 'react';
import { SmokingSpotWithDistance } from '@/types';
import { useFavorites } from '@/contexts/FavoritesContext';
import { useHistory } from '@/contexts/HistoryContext';
import RouteGuide from './RouteGuide';
import ReviewSystem from './ReviewSystem';

interface SpotListProps {
  spots: SmokingSpotWithDistance[];
  onSpotSelect: (spot: SmokingSpotWithDistance) => void;
  selectedSpot: SmokingSpotWithDistance | null;
  userLocation: { lat: number; lng: number } | null;
}

const SpotList: React.FC<SpotListProps> = ({ 
  spots, 
  onSpotSelect, 
  selectedSpot,
  userLocation 
}) => {
  const { isFavorite, toggleFavorite } = useFavorites();
  const { addToHistory } = useHistory();
  const [showDetails, setShowDetails] = useState<number | null>(null);
  const [routeGuideSpot, setRouteGuideSpot] = useState<SmokingSpotWithDistance | null>(null);
  const [reviewSpot, setReviewSpot] = useState<SmokingSpotWithDistance | null>(null);

  const calculateDistance = (spot: SmokingSpotWithDistance) => {
    if (!userLocation) return null;
    
    const R = 6371; // 地球の半径（km）
    const dLat = (spot.lat - userLocation.lat) * Math.PI / 180;
    const dLon = (spot.lng - userLocation.lng) * Math.PI / 180;
    const a = 
      Math.sin(dLat/2) * Math.sin(dLat/2) +
      Math.cos(userLocation.lat * Math.PI / 180) * Math.cos(spot.lat * Math.PI / 180) * 
      Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    const distance = R * c;
    
    return distance < 1 ? `${Math.round(distance * 1000)}m` : `${distance.toFixed(1)}km`;
  };

  const getCategoryIcon = (category: string): string => {
    const icons: { [key: string]: string } = {
      'カフェ': '☕',
      'レストラン': '🍽️',
      'コンビニ': '🏪',
      '駅': '🚉',
      '公園': '🌳',
      '商業施設': '🏬',
      'その他': '📍'
    };
    return icons[category] || '📍';
  };

  const handleSpotSelect = (spot: SmokingSpotWithDistance) => {
    addToHistory(spot);
    onSpotSelect(spot);
  };

  return (
    <>
      <div className="space-y-4">
        {spots.map((spot) => {
          const distance = calculateDistance(spot);
          const isSelected = selectedSpot?.id === spot.id;
          const isFav = isFavorite(spot.id);
          
          return (
            <div
              key={spot.id}
              className={`card p-6 cursor-pointer transition-all duration-300 hover:shadow-lg ${
                isSelected 
                  ? 'ring-2 ring-blue-500 bg-blue-50 dark:bg-blue-900/20' 
                  : 'hover:bg-gray-50 dark:hover:bg-gray-800'
              }`}
              onClick={() => handleSpotSelect(spot)}
            >
              <div className="flex justify-between items-start">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <span className="text-lg" aria-hidden="true">{getCategoryIcon(spot.category)}</span>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                      {spot.name}
                    </h3>
                    <span className="px-2 py-1 text-xs bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200 rounded-full">
                      {spot.category}
                    </span>
                  </div>
                  
                  <p className="text-gray-600 dark:text-gray-400 mb-2">
                    {spot.address || '住所不明'}
                  </p>
                  
                  <div className="flex items-center gap-4 text-sm text-gray-500 dark:text-gray-400">
                    {distance && (
                      <span className="font-medium text-blue-600 dark:text-blue-400">
                        {distance}
                      </span>
                    )}
                    {spot.tags && (
                      <span className="text-xs text-gray-400">
                        {spot.tags}
                      </span>
                    )}
                  </div>

                  {showDetails === spot.id && (
                    <div className="mt-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <div className="space-y-2 text-sm">
                        <p><strong>詳細:</strong> {spot.description || '詳細情報なし'}</p>
                        <p><strong>カテゴリ:</strong> {spot.category}</p>
                        <p><strong>タグ:</strong> {spot.tags || 'なし'}</p>
                      </div>
                    </div>
                  )}
                </div>

                <div className="flex flex-col items-end gap-2">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setReviewSpot(spot);
                    }}
                    className="p-2 rounded-full bg-yellow-100 text-yellow-600 hover:bg-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-400 transition-all duration-200"
                    aria-label="レビューを表示"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
                    </svg>
                  </button>

                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setRouteGuideSpot(spot);
                    }}
                    className="p-2 rounded-full bg-green-100 text-green-600 hover:bg-green-200 dark:bg-green-900/30 dark:text-green-400 transition-all duration-200"
                    aria-label="経路案内を表示"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-1.447-.894L15 4m0 13V4m-6 3l6-3" />
                    </svg>
                  </button>

                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleFavorite(spot);
                    }}
                    className={`p-2 rounded-full transition-all duration-200 ${
                      isFav 
                        ? 'bg-red-100 text-red-600 hover:bg-red-200 dark:bg-red-900/30 dark:text-red-400' 
                        : 'bg-gray-100 text-gray-400 hover:bg-gray-200 hover:text-gray-600 dark:bg-gray-800 dark:text-gray-500 dark:hover:bg-gray-700'
                    }`}
                    aria-label={isFav ? 'お気に入りから削除' : 'お気に入りに追加'}
                  >
                    <svg className="w-5 h-5" fill={isFav ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                    </svg>
                  </button>

                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setShowDetails(showDetails === spot.id ? null : spot.id);
                    }}
                    className="p-2 rounded-full bg-gray-100 text-gray-400 hover:bg-gray-200 hover:text-gray-600 dark:bg-gray-800 dark:text-gray-500 dark:hover:bg-gray-700 transition-all duration-200"
                    aria-label="詳細を表示"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {routeGuideSpot && (
        <RouteGuide
          spot={routeGuideSpot}
          userLocation={userLocation}
          onClose={() => setRouteGuideSpot(null)}
        />
      )}

      {reviewSpot && (
        <ReviewSystem
          spot={reviewSpot}
          onClose={() => setReviewSpot(null)}
        />
      )}
    </>
  );
};

export default SpotList; 