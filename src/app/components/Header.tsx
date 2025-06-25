"use client";
import React, { useState } from 'react';

const Header: React.FC = () => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [showStats, setShowStats] = useState(false);
  const [stats, setStats] = useState({
    totalSpots: 0,
    smokingAreas: 0,
    restaurants: 0,
    totalPhotos: 0,
    totalFeedbacks: 0
  });

  const handleStatsClick = async () => {
    try {
      // 統計データを取得
      const [spotsRes, photosRes, feedbackRes] = await Promise.all([
        fetch('/api/spots'),
        fetch('/api/photos'),
        fetch('/api/feedback')
      ]);
      
      const spots = await spotsRes.json();
      const photos = await photosRes.json();
      const feedbacks = await feedbackRes.json();
      
      setStats({
        totalSpots: spots.length,
        smokingAreas: spots.filter((s: any) => s.category === '喫煙所').length,
        restaurants: spots.filter((s: any) => s.category === '飲食店').length,
        totalPhotos: photos.length,
        totalFeedbacks: feedbacks.length
      });
      
      setShowStats(true);
    } catch (error) {
      console.error('統計データ取得エラー:', error);
      setShowStats(true); // エラーでもモーダルは表示
    }
  };

  return (
    <>
      <header className="bg-white shadow-sm border-b border-gray-200 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            {/* ロゴ */}
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-xl flex items-center justify-center">
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </div>
              <div>
                <h1 className="text-xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                  Smokers Oasis
                </h1>
                <p className="text-xs text-gray-500">喫煙所・喫煙可能店舗マップ</p>
              </div>
            </div>

            {/* デスクトップナビゲーション */}
            <nav className="hidden md:flex items-center space-x-8">
              <a href="#" className="text-gray-700 hover:text-blue-600 transition-colors font-medium">
                ホーム
              </a>
              <a href="#" className="text-gray-700 hover:text-blue-600 transition-colors font-medium">
                お気に入り
              </a>
              <a href="#" className="text-gray-700 hover:text-blue-600 transition-colors font-medium">
                新着情報
              </a>
              <a href="#" className="text-gray-700 hover:text-blue-600 transition-colors font-medium">
                ヘルプ
              </a>
            </nav>

            {/* アクションボタン */}
            <div className="hidden md:flex items-center space-x-4">
              <button 
                onClick={handleStatsClick}
                className="btn btn-ghost"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-5 5v-5z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 002 2z" />
                </svg>
                <span className="ml-2">統計</span>
              </button>
              <button className="btn btn-primary">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                </svg>
                <span className="ml-2">新規登録</span>
              </button>
            </div>

            {/* モバイルメニューボタン */}
            <div className="md:hidden">
              <button
                onClick={() => setIsMenuOpen(!isMenuOpen)}
                className="p-2 rounded-lg text-gray-700 hover:text-blue-600 hover:bg-gray-100 transition-colors"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  {isMenuOpen ? (
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  ) : (
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                  )}
                </svg>
              </button>
            </div>
          </div>

          {/* モバイルメニュー */}
          {isMenuOpen && (
            <div className="md:hidden animate-fade-in">
              <div className="px-2 pt-2 pb-3 space-y-1 bg-white border-t border-gray-200">
                <a href="#" className="block px-3 py-2 text-gray-700 hover:text-blue-600 hover:bg-gray-50 rounded-lg transition-colors">
                  ホーム
                </a>
                <a href="#" className="block px-3 py-2 text-gray-700 hover:text-blue-600 hover:bg-gray-50 rounded-lg transition-colors">
                  お気に入り
                </a>
                <a href="#" className="block px-3 py-2 text-gray-700 hover:text-blue-600 hover:bg-gray-50 rounded-lg transition-colors">
                  新着情報
                </a>
                <a href="#" className="block px-3 py-2 text-gray-700 hover:text-blue-600 hover:bg-gray-50 rounded-lg transition-colors">
                  ヘルプ
                </a>
                <div className="pt-4 space-y-2">
                  <button 
                    onClick={handleStatsClick}
                    className="w-full btn btn-ghost justify-start"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-5 5v-5z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 002 2z" />
                    </svg>
                    <span className="ml-2">統計</span>
                  </button>
                  <button className="w-full btn btn-primary justify-start">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                    </svg>
                    <span className="ml-2">新規登録</span>
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </header>

      {/* 統計モーダル */}
      {showStats && (
        <div className="modal-overlay fixed inset-0 flex items-center justify-center z-50">
          <div className="modal-content card p-6 max-w-md w-full mx-4">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-bold text-gray-800">📊 統計情報</h3>
              <button
                onClick={() => setShowStats(false)}
                className="text-gray-400 hover:text-gray-600"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="card p-4 text-center">
                  <div className="text-2xl font-bold text-blue-600">{stats.totalSpots}</div>
                  <div className="text-sm text-gray-600">総スポット数</div>
                </div>
                <div className="card p-4 text-center">
                  <div className="text-2xl font-bold text-green-600">{stats.smokingAreas}</div>
                  <div className="text-sm text-gray-600">喫煙所</div>
                </div>
                <div className="card p-4 text-center">
                  <div className="text-2xl font-bold text-purple-600">{stats.restaurants}</div>
                  <div className="text-sm text-gray-600">飲食店</div>
                </div>
                <div className="card p-4 text-center">
                  <div className="text-2xl font-bold text-yellow-600">{stats.totalPhotos}</div>
                  <div className="text-sm text-gray-600">写真数</div>
                </div>
              </div>
              
              <div className="card p-4 text-center">
                <div className="text-2xl font-bold text-orange-600">{stats.totalFeedbacks}</div>
                <div className="text-sm text-gray-600">フィードバック数</div>
              </div>
              
              <div className="text-xs text-gray-500 text-center">
                最終更新: {new Date().toLocaleString('ja-JP')}
              </div>
            </div>
            
            <div className="mt-6">
              <button
                onClick={() => setShowStats(false)}
                className="btn btn-primary w-full"
              >
                閉じる
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default Header; 