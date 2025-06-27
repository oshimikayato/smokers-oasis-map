"use client";
import React, { useState } from 'react';
import { SmokingSpotWithDistance } from '@/types';

interface Review {
  id: number;
  spotId: number;
  rating: number;
  comment: string;
  userName: string;
  createdAt: Date;
}

interface ReviewSystemProps {
  spot: SmokingSpotWithDistance;
  onClose: () => void;
}

const ReviewSystem: React.FC<ReviewSystemProps> = ({ spot, onClose }) => {
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');
  const [userName, setUserName] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [reviews, setReviews] = useState<Review[]>([]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (rating === 0) {
      alert('評価を選択してください');
      return;
    }

    if (!comment.trim()) {
      alert('コメントを入力してください');
      return;
    }

    if (!userName.trim()) {
      alert('お名前を入力してください');
      return;
    }

    setIsSubmitting(true);

    try {
      const newReview: Review = {
        id: Date.now(),
        spotId: spot.id,
        rating,
        comment: comment.trim(),
        userName: userName.trim(),
        createdAt: new Date()
      };

      // ローカルストレージに保存
      const existingReviews = JSON.parse(localStorage.getItem(`reviews_${spot.id}`) || '[]');
      const updatedReviews = [...existingReviews, newReview];
      localStorage.setItem(`reviews_${spot.id}`, JSON.stringify(updatedReviews));

      setReviews(updatedReviews);
      setRating(0);
      setComment('');
      setUserName('');
      
      alert('レビューを投稿しました！');
    } catch (error) {
      console.error('レビューの投稿に失敗しました:', error);
      alert('レビューの投稿に失敗しました');
    } finally {
      setIsSubmitting(false);
    }
  };

  const getAverageRating = () => {
    if (reviews.length === 0) return 0;
    const total = reviews.reduce((sum, review) => sum + review.rating, 0);
    return total / reviews.length;
  };

  const renderStars = (rating: number, interactive = false) => {
    return (
      <div className="flex gap-1">
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            type={interactive ? 'button' : undefined}
            onClick={interactive ? () => setRating(star) : undefined}
            className={`text-2xl ${interactive ? 'cursor-pointer' : ''} ${
              star <= rating ? 'text-yellow-400' : 'text-gray-300'
            }`}
            disabled={!interactive}
          >
            ★
          </button>
        ))}
      </div>
    );
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-800 rounded-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto p-6 shadow-xl">
        <div className="flex justify-between items-center mb-6">
          <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
            ⭐ レビュー・評価
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

        <div className="space-y-6">
          {/* スポット情報 */}
          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-2">
              {spot.name}
            </h4>
            <p className="text-sm text-gray-600 dark:text-gray-400">
              {spot.address || '住所不明'}
            </p>
            <div className="mt-2">
              <p className="text-sm text-gray-600 dark:text-gray-400">
                平均評価: {getAverageRating().toFixed(1)} / 5.0 ({reviews.length}件のレビュー)
              </p>
              {reviews.length > 0 && (
                <div className="mt-1">
                  {renderStars(getAverageRating())}
                </div>
              )}
            </div>
          </div>

          {/* レビュー投稿フォーム */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <h4 className="font-medium text-gray-900 dark:text-gray-100">
              レビューを投稿
            </h4>
            
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                お名前
              </label>
              <input
                type="text"
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent dark:bg-gray-700 dark:text-white"
                placeholder="お名前を入力"
                maxLength={50}
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                評価
              </label>
              {renderStars(rating, true)}
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                星をクリックして評価を選択してください
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                コメント
              </label>
              <textarea
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent dark:bg-gray-700 dark:text-white"
                rows={4}
                placeholder="このスポットについての感想を教えてください"
                maxLength={500}
                required
              />
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                {comment.length}/500文字
              </p>
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isSubmitting ? '投稿中...' : 'レビューを投稿'}
            </button>
          </form>

          {/* 既存のレビュー */}
          {reviews.length > 0 && (
            <div>
              <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-4">
                レビュー一覧
              </h4>
              <div className="space-y-4 max-h-64 overflow-y-auto">
                {reviews.map((review) => (
                  <div key={review.id} className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                    <div className="flex justify-between items-start mb-2">
                      <div>
                        <p className="font-medium text-gray-900 dark:text-gray-100">
                          {review.userName}
                        </p>
                        <p className="text-sm text-gray-500 dark:text-gray-400">
                          {new Date(review.createdAt).toLocaleDateString('ja-JP')}
                        </p>
                      </div>
                      {renderStars(review.rating)}
                    </div>
                    <p className="text-gray-700 dark:text-gray-300 text-sm">
                      {review.comment}
                    </p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ReviewSystem; 