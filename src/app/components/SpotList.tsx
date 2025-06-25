"use client";
import React from 'react';

interface SmokingSpot {
  id: number;
  name: string;
  lat: number;
  lng: number;
  address?: string;
  description?: string;
  category: string;
  tags: string[];
  distance?: number;
}

interface SpotListProps {
  spots: SmokingSpot[];
  favorites: number[];
  onToggleFavorite: (spotId: number) => void;
  onSelectSpot: (spot: SmokingSpot) => void;
  userLocation: { lat: number; lng: number } | null;
}

const SpotList: React.FC<SpotListProps> = ({
  spots,
  favorites,
  onToggleFavorite,
  onSelectSpot,
  userLocation
}) => {
  const formatDistance = (distance?: number) => {
    if (!distance) return null;
    if (distance < 1) {
      return `${Math.round(distance * 1000)}m`;
    }
    return `${distance.toFixed(1)}km`;
  };

  const getCategoryIcon = (category: string) => {
    return category === "喫煙所" ? "🚬" : "🍽️";
  };

  return (
    <div className="bg-white rounded-lg shadow-lg overflow-hidden">
      <div className="p-4 border-b border-gray-200">
        <h3 className="text-lg font-semibold text-gray-900">
          📍 スポット一覧 ({spots.length}件)
        </h3>
        {userLocation && (
          <p className="text-sm text-gray-600 mt-1">
            現在地からの距離で表示中
          </p>
        )}
      </div>
      
      <div className="max-h-96 overflow-y-auto">
        {spots.length === 0 ? (
          <div className="p-6 text-center text-gray-500">
            <svg className="w-12 h-12 mx-auto mb-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <p>条件に一致するスポットが見つかりません</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {spots.map((spot) => (
              <div
                key={spot.id}
                className="p-4 hover:bg-gray-50 transition-colors cursor-pointer"
                onClick={() => onSelectSpot(spot)}
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="text-lg">{getCategoryIcon(spot.category)}</span>
                      <h4 className="text-sm font-medium text-gray-900 truncate">
                        {spot.name}
                      </h4>
                      <span className="text-xs text-gray-500 bg-gray-100 px-2 py-1 rounded">
                        {spot.category}
                      </span>
                    </div>
                    
                    {spot.address && (
                      <p className="text-xs text-gray-600 mb-2 truncate">
                        📍 {spot.address}
                      </p>
                    )}
                    
                    {spot.distance && userLocation && (
                      <p className="text-xs text-blue-600 font-medium mb-2">
                        📏 {formatDistance(spot.distance)}
                      </p>
                    )}
                    
                    {spot.description && (
                      <p className="text-xs text-gray-600 mb-2 line-clamp-2">
                        {spot.description}
                      </p>
                    )}
                    
                    <div className="flex flex-wrap gap-1">
                      {spot.tags.slice(0, 3).map((tag) => (
                        <span
                          key={tag}
                          className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded"
                        >
                          {tag}
                        </span>
                      ))}
                      {spot.tags.length > 3 && (
                        <span className="text-xs text-gray-500">
                          +{spot.tags.length - 3}
                        </span>
                      )}
                    </div>
                  </div>
                  
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onToggleFavorite(spot.id);
                    }}
                    className={`ml-3 p-2 rounded-full transition-colors ${
                      favorites.includes(spot.id)
                        ? 'text-red-500 hover:text-red-600'
                        : 'text-gray-400 hover:text-gray-600'
                    }`}
                  >
                    <svg
                      className="w-5 h-5"
                      fill={favorites.includes(spot.id) ? "currentColor" : "none"}
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
                      />
                    </svg>
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default SpotList; 