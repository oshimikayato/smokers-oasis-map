'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';

interface Spot {
  id: number;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  category: string;
  tags: string[];
  createdAt: string;
  updatedAt: string;
}

interface AdminStats {
  totalSpots: number;
  totalFeedbacks: number;
  totalPhotos: number;
  recentSpots: Spot[];
  recentFeedbacks: any[];
}

export default function AdminDashboard() {
  const [showLogin, setShowLogin] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [loginForm, setLoginForm] = useState({ username: '', password: '' });
  const [loginError, setLoginError] = useState('');
  const router = useRouter();

  const loadStats = useCallback(async () => {
    try {
      const [spotsRes, feedbacksRes, photosRes] = await Promise.all([
        fetch('/api/spots'),
        fetch('/api/feedback'),
        fetch('/api/photos')
      ]);

      const spots = await spotsRes.json();
      const feedbacks = await feedbacksRes.json();
      const photos = await photosRes.json();

      setStats({
        totalSpots: spots.length,
        totalFeedbacks: feedbacks.length,
        totalPhotos: photos.length,
        recentSpots: spots.slice(-5).reverse(),
        recentFeedbacks: feedbacks.slice(-5).reverse()
      });
    } catch (error) {
      console.error('Stats load error:', error);
    }
  }, []);

  const checkAuth = useCallback(async () => {
    try {
      const response = await fetch('/api/auth/verify');
      const data = await response.json();
      
      if (data.authenticated) {
        loadStats();
      } else {
        setShowLogin(true);
      }
    } catch (error) {
      console.error('Auth check error:', error);
      setShowLogin(true);
    } finally {
      setIsLoading(false);
    }
  }, [loadStats]);

  // 認証状態をチェック
  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoginError('');

    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(loginForm)
      });

      const data = await response.json();

      if (data.success) {
        loadStats();
      } else {
        setLoginError(data.message);
      }
    } catch (error) {
      setLoginError('ログイン処理中にエラーが発生しました');
    }
  };

  const handleLogout = async () => {
    try {
      await fetch('/api/auth/logout', { method: 'POST' });
      setStats(null);
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="spinner"></div>
      </div>
    );
  }

  if (showLogin) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="max-w-md w-full space-y-8 p-8 bg-white rounded-lg shadow-lg">
          <div className="text-center">
            <h2 className="text-3xl font-bold text-gray-900">システム管理</h2>
            <p className="mt-2 text-gray-600">yourbreakspot.com システム設定</p>
          </div>
          
          <form onSubmit={handleLogin} className="space-y-6">
            <div>
              <label className="block text-sm font-medium text-gray-700">
                ユーザー名
              </label>
              <input
                type="text"
                value={loginForm.username}
                onChange={(e) => setLoginForm({ ...loginForm, username: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                required
              />
            </div>
            
            <div>
              <label className="block text-sm font-medium text-gray-700">
                パスワード
              </label>
              <input
                type="password"
                value={loginForm.password}
                onChange={(e) => setLoginForm({ ...loginForm, password: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                required
              />
            </div>

            {loginError && (
              <div className="text-red-600 text-sm">{loginError}</div>
            )}

            <button
              type="submit"
              className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              ログイン
            </button>
          </form>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* ヘッダー */}
      <div className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">システム設定</h1>
              <p className="text-gray-600">yourbreakspot.com</p>
            </div>
            <div className="flex items-center space-x-4">
              <button
                onClick={() => router.push('/')}
                className="btn btn-ghost"
              >
                サイトに戻る
              </button>
              <button
                onClick={handleLogout}
                className="btn btn-outline"
              >
                ログアウト
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* メインコンテンツ */}
      <div className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        {stats && (
          <div className="space-y-6">
            {/* 統計カード */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-blue-500 rounded-md flex items-center justify-center">
                        <span className="text-white text-lg">🚬</span>
                      </div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          総喫煙所数
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {stats.totalSpots}件
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-green-500 rounded-md flex items-center justify-center">
                        <span className="text-white text-lg">💬</span>
                      </div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          総フィードバック数
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {stats.totalFeedbacks}件
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-purple-500 rounded-md flex items-center justify-center">
                        <span className="text-white text-lg">📷</span>
                      </div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          総写真数
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {stats.totalPhotos}件
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* 最近の投稿 */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <div className="bg-white shadow rounded-lg">
                <div className="px-4 py-5 sm:p-6">
                  <h3 className="text-lg leading-6 font-medium text-gray-900 mb-4">
                    最近の喫煙所登録
                  </h3>
                  <div className="space-y-3">
                    {stats.recentSpots.map((spot: any) => (
                      <div key={spot.id} className="border-l-4 border-blue-400 pl-4">
                        <div className="font-medium text-gray-900">{spot.name}</div>
                        <div className="text-sm text-gray-500">{spot.address}</div>
                        <div className="text-xs text-gray-400">
                          {new Date(spot.createdAt).toLocaleDateString('ja-JP')}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>

              <div className="bg-white shadow rounded-lg">
                <div className="px-4 py-5 sm:p-6">
                  <h3 className="text-lg leading-6 font-medium text-gray-900 mb-4">
                    最近のフィードバック
                  </h3>
                  <div className="space-y-3">
                    {stats.recentFeedbacks.map((feedback: any) => (
                      <div key={feedback.id} className="border-l-4 border-green-400 pl-4">
                        <div className="text-sm text-gray-900">
                          {feedback.comment ? feedback.comment.substring(0, 50) + '...' : 'コメントなし'}
                        </div>
                        <div className="text-xs text-gray-400">
                          {new Date(feedback.createdAt).toLocaleDateString('ja-JP')}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
} 