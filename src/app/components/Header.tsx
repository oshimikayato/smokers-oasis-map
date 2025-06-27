"use client";
import React, { useState } from 'react';
import Link from 'next/link';
import { useTheme } from '@/contexts/ThemeContext';

const Header: React.FC = () => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [showStats, setShowStats] = useState(false);
  const [stats] = useState({
    totalSpots: 0,
    smokingAreas: 0,
    restaurants: 0,
    totalPhotos: 0,
    totalFeedbacks: 0
  });
  const { theme, toggleTheme } = useTheme();

  return (
    <>
      <header className="bg-white/80 backdrop-blur-md shadow-lg border-b border-white/20 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-20">
            {/* ロゴ */}
            <div className="flex items-center space-x-4">
              <Link href="/" className="flex items-center space-x-4 group">
                <div className="w-12 h-12 bg-gradient-to-br from-blue-500 via-purple-500 to-indigo-600 rounded-2xl flex items-center justify-center shadow-lg group-hover:shadow-xl transition-all duration-300 group-hover:scale-105">
                  <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                </div>
                <div>
                  <h1 className="text-2xl font-bold bg-gradient-to-r from-blue-600 via-purple-600 to-indigo-600 bg-clip-text text-transparent">
                    Smokers Oasis
                  </h1>
                  <p className="text-sm text-gray-600 font-medium">喫煙所・喫煙可能店舗マップ</p>
                </div>
              </Link>
            </div>

            {/* デスクトップナビゲーション */}
            <nav className="hidden md:flex items-center space-x-8">
              <Link href="/" className="text-gray-700 hover:text-blue-600 transition-colors">
                ホーム
              </Link>
              <Link href="/favorites" className="text-gray-700 hover:text-blue-600 transition-colors">
                お気に入り
              </Link>
              <Link href="/history" className="text-gray-700 hover:text-blue-600 transition-colors">
                履歴
              </Link>
              <Link href="/stats" className="text-gray-700 hover:text-blue-600 transition-colors">
                統計
              </Link>
              <Link href="/quit-challenge" className="text-gray-700 hover:text-blue-600 transition-colors">
                禁煙チャレンジ
              </Link>
            </nav>

            {/* アクションボタン */}
            <div className="hidden md:flex items-center space-x-4">
              <Link 
                href="/stats"
                className="px-4 py-2 bg-gradient-to-r from-blue-50 to-indigo-50 text-blue-700 rounded-xl font-semibold hover:from-blue-100 hover:to-indigo-100 transition-all duration-300 shadow-md hover:shadow-lg flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-5 5v-5z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 002 2z" />
                </svg>
                <span>統計</span>
              </Link>
              <button className="px-6 py-2 bg-gradient-to-r from-blue-500 to-purple-600 text-white rounded-xl font-semibold hover:from-blue-600 hover:to-purple-700 transition-all duration-300 shadow-lg hover:shadow-xl transform hover:scale-105 flex items-center gap-2">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                </svg>
                <span>新規登録</span>
              </button>
              <button
                onClick={toggleTheme}
                className="p-2 rounded-xl bg-gray-100 hover:bg-gray-200 dark:bg-gray-800 dark:hover:bg-gray-700 transition-all duration-200"
                aria-label={theme === 'dark' ? 'ライトモードに切り替え' : 'ダークモードに切り替え'}
              >
                {theme === 'dark' ? (
                  <svg className="w-5 h-5 text-yellow-500" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95l.707.707a1 1 0 001.414-1.414l-.707-.707a1 1 0 00-1.414 1.414zm2.12-10.607a1 1 0 010 1.414l-.706.707a1 1 0 11-1.414-1.414l.707-.707a1 1 0 011.414 0zM17 11a1 1 0 100-2h-1a1 1 0 100 2h1zm-7 4a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zM5.05 6.464A1 1 0 106.465 5.05l-.708-.707a1 1 0 00-1.414 1.414l.707.707zm1.414 8.486l-.707.707a1 1 0 01-1.414-1.414l.707-.707a1 1 0 011.414 1.414zM4 11a1 1 0 100-2H3a1 1 0 000 2h1z" clipRule="evenodd" />
                  </svg>
                ) : (
                  <svg className="w-5 h-5 text-gray-700" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z" />
                  </svg>
                )}
              </button>
            </div>

            {/* モバイルメニューボタン */}
            <div className="md:hidden">
              <button
                onClick={() => setIsMenuOpen(!isMenuOpen)}
                className="p-3 rounded-xl text-gray-700 hover:text-blue-600 hover:bg-blue-50 transition-all duration-300"
                aria-expanded={isMenuOpen}
                aria-controls="mobile-menu"
                aria-label={isMenuOpen ? "メニューを閉じる" : "メニューを開く"}
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
            <div className="md:hidden animate-fade-in" id="mobile-menu" role="navigation" aria-label="モバイルメニュー">
              <div className="px-2 pt-2 pb-6 space-y-2 bg-white/80 backdrop-blur-md border-t border-white/20 rounded-b-3xl">
                <Link href="/" className="block px-4 py-3 text-gray-700 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all duration-300 font-semibold">
                  ホーム
                </Link>
                <Link href="/favorites" className="block px-4 py-3 text-gray-700 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all duration-300 font-semibold">
                  お気に入り
                </Link>
                <Link href="/history" className="block px-4 py-3 text-gray-700 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all duration-300 font-semibold">
                  履歴
                </Link>
                <Link href="/quit-challenge" className="block px-4 py-3 text-gray-700 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all duration-300 font-semibold">
                  禁煙チャレンジ
                </Link>
                <div className="pt-4 space-y-3 border-t border-gray-200">
                  <Link 
                    href="/stats"
                    className="w-full px-4 py-3 bg-gradient-to-r from-blue-50 to-indigo-50 text-blue-700 rounded-xl font-semibold hover:from-blue-100 hover:to-indigo-100 transition-all duration-300 flex items-center gap-3"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-5 5v-5z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 002 2z" />
                    </svg>
                    <span>統計</span>
                  </Link>
                  <button className="w-full px-4 py-3 bg-gradient-to-r from-blue-500 to-purple-600 text-white rounded-xl font-semibold hover:from-blue-600 hover:to-purple-700 transition-all duration-300 flex items-center gap-3">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                    </svg>
                    <span>新規登録</span>
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </header>

      {/* 統計モーダル */}
      {showStats && (
        <div 
          className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50" 
          role="dialog" 
          aria-modal="true"
          aria-labelledby="stats-modal-title"
        >
          <div className="bg-white/90 backdrop-blur-md rounded-3xl p-8 max-w-md w-full mx-4 shadow-2xl border border-white/30">
            <div className="flex items-center justify-between mb-8">
              <h3 id="stats-modal-title" className="text-2xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                📊 統計情報
              </h3>
              <button
                onClick={() => setShowStats(false)}
                className="text-gray-400 hover:text-gray-600 transition-colors p-2 rounded-xl hover:bg-gray-100"
                aria-label="統計モーダルを閉じる"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            
            <div className="space-y-6">
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-gradient-to-br from-blue-50 to-indigo-50 p-6 rounded-2xl text-center border border-blue-100">
                  <div className="text-3xl font-bold text-blue-600 mb-2">{stats.totalSpots}</div>
                  <div className="text-sm text-gray-600 font-semibold">総スポット数</div>
                </div>
                <div className="bg-gradient-to-br from-green-50 to-emerald-50 p-6 rounded-2xl text-center border border-green-100">
                  <div className="text-3xl font-bold text-green-600 mb-2">{stats.smokingAreas}</div>
                  <div className="text-sm text-gray-600 font-semibold">喫煙所</div>
                </div>
                <div className="bg-gradient-to-br from-purple-50 to-violet-50 p-6 rounded-2xl text-center border border-purple-100">
                  <div className="text-3xl font-bold text-purple-600 mb-2">{stats.restaurants}</div>
                  <div className="text-sm text-gray-600 font-semibold">飲食店</div>
                </div>
                <div className="bg-gradient-to-br from-yellow-50 to-orange-50 p-6 rounded-2xl text-center border border-yellow-100">
                  <div className="text-3xl font-bold text-yellow-600 mb-2">{stats.totalPhotos}</div>
                  <div className="text-sm text-gray-600 font-semibold">写真数</div>
                </div>
              </div>
              
              <div className="bg-gradient-to-br from-orange-50 to-red-50 p-6 rounded-2xl text-center border border-orange-100">
                <div className="text-3xl font-bold text-orange-600 mb-2">{stats.totalFeedbacks}</div>
                <div className="text-sm text-gray-600 font-semibold">フィードバック数</div>
              </div>
              
              <div className="text-xs text-gray-500 text-center bg-gray-50 p-4 rounded-xl">
                最終更新: {new Date().toLocaleString('ja-JP')}
              </div>
            </div>
            
            <div className="mt-8">
              <button
                onClick={() => setShowStats(false)}
                className="w-full px-6 py-3 bg-gradient-to-r from-blue-500 to-purple-600 text-white font-semibold rounded-xl hover:from-blue-600 hover:to-purple-700 transition-all duration-300 shadow-lg hover:shadow-xl transform hover:scale-105"
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