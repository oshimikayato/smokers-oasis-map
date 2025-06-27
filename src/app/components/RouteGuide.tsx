"use client";
import React, { useState } from 'react';
import { SmokingSpotWithDistance } from '@/types';

interface RouteGuideProps {
  spot: SmokingSpotWithDistance;
  userLocation: { lat: number; lng: number } | null;
  onClose: () => void;
}

const RouteGuide: React.FC<RouteGuideProps> = ({ spot, userLocation, onClose }) => {
  const [isLoading, setIsLoading] = useState(false);

  const openGoogleMaps = () => {
    if (!userLocation) {
      alert('現在地が取得できません。位置情報の許可を確認してください。');
      return;
    }

    const origin = `${userLocation.lat},${userLocation.lng}`;
    const destination = `${spot.lat},${spot.lng}`;
    const url = `https://www.google.com/maps/dir/${origin}/${destination}`;
    window.open(url, '_blank');
  };

  const openAppleMaps = () => {
    if (!userLocation) {
      alert('現在地が取得できません。位置情報の許可を確認してください。');
      return;
    }

    const url = `http://maps.apple.com/?saddr=${userLocation.lat},${userLocation.lng}&daddr=${spot.lat},${spot.lng}`;
    window.open(url, '_blank');
  };

  const copyAddress = async () => {
    try {
      await navigator.clipboard.writeText(spot.address || '');
      alert('住所をコピーしました');
    } catch (error) {
      console.error('コピーに失敗しました:', error);
      alert('コピーに失敗しました');
    }
  };

  const calculateDistance = () => {
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

  const distance = calculateDistance();

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-800 rounded-2xl max-w-md w-full p-6 shadow-xl">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
            🗺️ 経路案内
          </h3>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="space-y-4">
          {/* スポット情報 */}
          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-2">
              {spot.name}
            </h4>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
              {spot.address || '住所不明'}
            </p>
            {distance && (
              <p className="text-sm text-blue-600 dark:text-blue-400 font-medium">
                距離: {distance}
              </p>
            )}
          </div>

          {/* 経路案内オプション */}
          <div className="space-y-3">
            <h4 className="font-medium text-gray-900 dark:text-gray-100">
              経路案内を開く
            </h4>
            
            <button
              onClick={openGoogleMaps}
              className="w-full flex items-center justify-center gap-3 px-4 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
            >
              <svg className="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
              </svg>
              Google マップで開く
            </button>

            <button
              onClick={openAppleMaps}
              className="w-full flex items-center justify-center gap-3 px-4 py-3 bg-gray-800 text-white rounded-lg hover:bg-gray-900 transition-colors"
            >
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
              </svg>
              Apple マップで開く
            </button>

            <button
              onClick={copyAddress}
              className="w-full flex items-center justify-center gap-3 px-4 py-3 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
              </svg>
              住所をコピー
            </button>
          </div>

          {/* 注意事項 */}
          <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-3">
            <p className="text-xs text-yellow-800 dark:text-yellow-200">
              ⚠️ 外部アプリで経路案内を開きます。位置情報の許可が必要です。
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default RouteGuide; 