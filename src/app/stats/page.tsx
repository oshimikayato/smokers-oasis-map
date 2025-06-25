"use client";
import React, { useEffect, useState } from 'react';
import Link from 'next/link';

interface Stats {
  totalSpots: number;
  smokingAreas: number;
  restaurants: number;
  totalPhotos: number;
  totalFeedbacks: number;
  categoryBreakdown: { [key: string]: number };
  tagBreakdown: { [key: string]: number };
  recentActivity: {
    type: 'spot' | 'photo' | 'feedback';
    id: number;
    name: string;
    timestamp: string;
  }[];
  topRatedSpots: {
    id: number;
    name: string;
    rating: number;
    feedbackCount: number;
  }[];
}

const StatsPage: React.FC = () => {
  const [stats, setStats] = useState<Stats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [timeRange, setTimeRange] = useState<'week' | 'month' | 'year'>('month');

  useEffect(() => {
    fetchStats();
  }, [timeRange]);

  const fetchStats = async () => {
    try {
      setIsLoading(true);
      const [spotsRes, photosRes, feedbackRes] = await Promise.all([
        fetch('/api/spots'),
        fetch('/api/photos'),
        fetch('/api/feedback')
      ]);
      
      const spots = await spotsRes.json();
      const photos = await photosRes.json();
      const feedbacks = await feedbackRes.json();
      
      // カテゴリ別集計
      const categoryBreakdown = spots.reduce((acc: any, spot: any) => {
        acc[spot.category] = (acc[spot.category] || 0) + 1;
        return acc;
      }, {});

      // タグ別集計
      const tagBreakdown = spots.reduce((acc: any, spot: any) => {
        spot.tags.forEach((tag: string) => {
          acc[tag] = (acc[tag] || 0) + 1;
        });
        return acc;
      }, {});

      // 最近のアクティビティ
      const recentActivity = [
        ...spots.slice(0, 5).map((spot: any) => ({
          type: 'spot' as const,
          id: spot.id,
          name: spot.name,
          timestamp: new Date().toISOString()
        })),
        ...photos.slice(0, 3).map((photo: any) => ({
          type: 'photo' as const,
          id: photo.id,
          name: `写真投稿: ${photo.caption || '無題'}`,
          timestamp: photo.createdAt
        })),
        ...feedbacks.slice(0, 3).map((feedback: any) => ({
          type: 'feedback' as const,
          id: feedback.id,
          name: `フィードバック: ${feedback.comment?.substring(0, 30) || '評価のみ'}...`,
          timestamp: feedback.createdAt
        }))
      ].sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
       .slice(0, 10);

      // 高評価スポット
      const spotRatings = feedbacks.reduce((acc: any, feedback: any) => {
        if (feedback.rating && feedback.spotId) {
          if (!acc[feedback.spotId]) {
            acc[feedback.spotId] = { total: 0, count: 0 };
          }
          acc[feedback.spotId].total += feedback.rating;
          acc[feedback.spotId].count += 1;
        }
        return acc;
      }, {});

      const topRatedSpots = Object.entries(spotRatings)
        .map(([spotId, data]: [string, any]) => {
          const spot = spots.find((s: any) => s.id === parseInt(spotId));
          return {
            id: parseInt(spotId),
            name: spot?.name || '不明',
            rating: parseFloat((data.total / data.count).toFixed(1)),
            feedbackCount: data.count
          };
        })
        .sort((a, b) => b.rating - a.rating)
        .slice(0, 5);

      setStats({
        totalSpots: spots.length,
        smokingAreas: spots.filter((s: any) => s.category === '喫煙所').length,
        restaurants: spots.filter((s: any) => s.category === '飲食店').length,
        totalPhotos: photos.length,
        totalFeedbacks: feedbacks.length,
        categoryBreakdown,
        tagBreakdown,
        recentActivity,
        topRatedSpots
      });
    } catch (error) {
      console.error('統計データ取得エラー:', error);
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!stats) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-600">統計データの取得に失敗しました</p>
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
              <h1 className="text-2xl font-bold text-gray-900">📊 統計ダッシュボード</h1>
            </div>
            <div className="flex items-center gap-2">
              <select
                value={timeRange}
                onChange={(e) => setTimeRange(e.target.value as 'week' | 'month' | 'year')}
                className="px-3 py-1 border border-gray-300 rounded-md text-sm"
              >
                <option value="week">過去1週間</option>
                <option value="month">過去1ヶ月</option>
                <option value="year">過去1年</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* メインコンテンツ */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="space-y-8">
          {/* 主要統計 */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <div className="bg-white p-6 rounded-lg shadow-sm border">
              <div className="flex items-center">
                <div className="p-3 bg-blue-100 rounded-lg">
                  <svg className="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">総スポット数</p>
                  <p className="text-3xl font-bold text-gray-900">{stats.totalSpots}</p>
                </div>
              </div>
            </div>

            <div className="bg-white p-6 rounded-lg shadow-sm border">
              <div className="flex items-center">
                <div className="p-3 bg-green-100 rounded-lg">
                  <svg className="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                  </svg>
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">喫煙所</p>
                  <p className="text-3xl font-bold text-gray-900">{stats.smokingAreas}</p>
                </div>
              </div>
            </div>

            <div className="bg-white p-6 rounded-lg shadow-sm border">
              <div className="flex items-center">
                <div className="p-3 bg-purple-100 rounded-lg">
                  <svg className="w-8 h-8 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 15.546c-.523 0-1.046.151-1.5.454a2.704 2.704 0 01-3 0 2.704 2.704 0 00-3 0 2.704 2.704 0 01-3 0 2.704 2.704 0 00-3 0 2.701 2.701 0 00-1.5-.454M9 6v2m3-2v2m3-2v2M9 3h.01M12 3h.01M15 3h.01M21 21v-7a2 2 0 00-2-2H5a2 2 0 00-2 2v7h18z" />
                  </svg>
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">飲食店</p>
                  <p className="text-3xl font-bold text-gray-900">{stats.restaurants}</p>
                </div>
              </div>
            </div>

            <div className="bg-white p-6 rounded-lg shadow-sm border">
              <div className="flex items-center">
                <div className="p-3 bg-yellow-100 rounded-lg">
                  <svg className="w-8 h-8 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">写真数</p>
                  <p className="text-3xl font-bold text-gray-900">{stats.totalPhotos}</p>
                </div>
              </div>
            </div>
          </div>

          {/* 詳細統計 */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            {/* カテゴリ別分布 */}
            <div className="bg-white p-6 rounded-lg shadow-sm border">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">カテゴリ別分布</h3>
              <div className="space-y-3">
                {Object.entries(stats.categoryBreakdown).map(([category, count]) => (
                  <div key={category} className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">{category}</span>
                    <div className="flex items-center gap-2">
                      <div className="w-32 bg-gray-200 rounded-full h-2">
                        <div 
                          className="bg-blue-600 h-2 rounded-full" 
                          style={{ width: `${(count / stats.totalSpots) * 100}%` }}
                        ></div>
                      </div>
                      <span className="text-sm font-medium text-gray-900 w-8 text-right">
                        {count}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* 人気タグ */}
            <div className="bg-white p-6 rounded-lg shadow-sm border">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">人気タグ</h3>
              <div className="flex flex-wrap gap-2">
                {Object.entries(stats.tagBreakdown)
                  .sort(([,a], [,b]) => b - a)
                  .slice(0, 10)
                  .map(([tag, count]) => (
                    <div key={tag} className="flex items-center gap-1 px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm">
                      <span>{tag}</span>
                      <span className="text-xs bg-blue-200 px-1 rounded-full">{count}</span>
                    </div>
                  ))}
              </div>
            </div>
          </div>

          {/* 高評価スポット */}
          <div className="bg-white p-6 rounded-lg shadow-sm border">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">⭐ 高評価スポット</h3>
            <div className="space-y-3">
              {stats.topRatedSpots.map((spot, index) => (
                <div key={spot.id} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 bg-yellow-100 rounded-full flex items-center justify-center">
                      <span className="text-sm font-bold text-yellow-600">{index + 1}</span>
                    </div>
                    <div>
                      <p className="font-medium text-gray-900">{spot.name}</p>
                      <p className="text-sm text-gray-600">{spot.feedbackCount}件の評価</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="flex items-center">
                      {[...Array(5)].map((_, i) => (
                        <svg
                          key={i}
                          className={`w-4 h-4 ${
                            i < Math.floor(spot.rating) ? 'text-yellow-400 fill-current' : 'text-gray-300'
                          }`}
                          fill="currentColor"
                          viewBox="0 0 20 20"
                        >
                          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.77.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                        </svg>
                      ))}
                    </div>
                    <span className="text-lg font-bold text-gray-900">{spot.rating.toFixed(1)}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* 最近のアクティビティ */}
          <div className="bg-white p-6 rounded-lg shadow-sm border">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">📈 最近のアクティビティ</h3>
            <div className="space-y-3">
              {stats.recentActivity.map((activity, index) => (
                <div key={index} className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
                    activity.type === 'spot' ? 'bg-blue-100' :
                    activity.type === 'photo' ? 'bg-green-100' : 'bg-purple-100'
                  }`}>
                    {activity.type === 'spot' && (
                      <svg className="w-4 h-4 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                      </svg>
                    )}
                    {activity.type === 'photo' && (
                      <svg className="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                    )}
                    {activity.type === 'feedback' && (
                      <svg className="w-4 h-4 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                      </svg>
                    )}
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium text-gray-900">{activity.name}</p>
                    <p className="text-xs text-gray-500">
                      {new Date(activity.timestamp).toLocaleString('ja-JP')}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default StatsPage; 