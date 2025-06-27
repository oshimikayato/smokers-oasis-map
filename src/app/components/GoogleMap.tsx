"use client";
import React, { useEffect, useRef, useState, useCallback } from "react";
import SearchFilters from "./SearchFilters";
import SpotList from "./SpotList";
import LoadingSkeleton from "./LoadingSkeleton";
import ErrorMessage from "./ErrorMessage";
import { useSpots } from "@/hooks/useSpots";
import { SmokingSpot, Feedback, FeedbackForm, PhotoForm } from "@/types";

// Google Maps APIの型定義
declare global {
  interface Window {
    google: any;
    markers?: any[];
  }
}

interface GoogleMapProps {
  showSearchFilters?: boolean;
  showSpotList?: boolean;
  onSpotSelect?: (spot: SmokingSpot) => void;
}

const GoogleMap: React.FC<GoogleMapProps> = ({
  showSearchFilters = true,
  showSpotList = false,
  onSpotSelect
}) => {
  const mapRef = useRef<HTMLDivElement>(null);
  const [selectedSpot, setSelectedSpot] = useState<SmokingSpot | null>(null);
  const [feedbacks, setFeedbacks] = useState<Feedback[]>([]);
  const [feedbackForm, setFeedbackForm] = useState<FeedbackForm>({ found: undefined, rating: 0, comment: "", reportType: "" });
  const [showForm, setShowForm] = useState(false);
  const [map, setMap] = useState<any>(null);
  const [showPhotoUpload, setShowPhotoUpload] = useState(false);
  const [photoForm, setPhotoForm] = useState<PhotoForm>({ url: "", caption: "" });

  // カスタムフックを使用
  const {
    filteredSpots,
    isLoading,
    error,
    userLocation,
    search,
    setSearch,
    categoryFilter,
    setCategoryFilter,
    tagFilters,
    setTagFilters,
    sortBy,
    setSortBy,
    resetFilters,
    getUserLocation
  } = useSpots();

  // お気に入り機能
  const [favorites, setFavorites] = useState<number[]>(() => {
    if (typeof window !== "undefined") {
      const fav = localStorage.getItem("favorites");
      return fav ? JSON.parse(fav) : [];
    }
    return [];
  });

  // お気に入り保存をメモ化
  const saveFavorites = useCallback(() => {
    if (typeof window !== "undefined") {
      localStorage.setItem("favorites", JSON.stringify(favorites));
    }
  }, [favorites]);

  useEffect(() => {
    saveFavorites();
  }, [saveFavorites]);

  // お気に入り切り替えをメモ化
  const toggleFavorite = useCallback((spotId: number) => {
    setFavorites(favs => favs.includes(spotId) ? favs.filter(id => id !== spotId) : [...favs, spotId]);
  }, []);

  // スポット選択ハンドラー
  const handleSpotSelect = useCallback((spot: SmokingSpot) => {
    setSelectedSpot(spot);
    if (onSpotSelect) {
      onSpotSelect(spot);
    }
  }, [onSpotSelect]);

  // Google Maps APIの読み込み
  useEffect(() => {
    const loadGoogleMapsAPI = () => {
      if (window.google && window.google.maps) {
        return; // 既にロード済み
      }

      // Google Maps APIが既にロードされているかチェック
      const existingScript = document.querySelector('script[src*="maps.googleapis.com"]');
      if (existingScript) {
        return; // 既にロード中またはロード済み
      }

      // 新しいスクリプトをロード
      const script = document.createElement("script");
      script.src = `https://maps.googleapis.com/maps/api/js?key=${process.env['NEXT_PUBLIC_GOOGLE_MAPS_API_KEY'] || ''}&libraries=places`;
      script.async = true;
      script.defer = true;
      script.onerror = () => {
        console.error('Google Maps APIの読み込みに失敗しました');
      };
      document.head.appendChild(script);
    };

    if (typeof window !== "undefined") {
      loadGoogleMapsAPI();
    }
  }, []);

  // マップの初期化（Google Maps APIとmapRefが利用可能になったら実行）
  useEffect(() => {
    const checkAndInitialize = () => {
      if (window.google && window.google.maps && mapRef.current) {
        console.log('Initializing map - both Google Maps API and mapRef are available');
        initializeMap();
      } else {
        // まだ準備ができていない場合は少し待ってから再試行
        setTimeout(checkAndInitialize, 100);
      }
    };

    checkAndInitialize();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // マップ初期化関数
  const initializeMap = useCallback(() => {
    console.log('initializeMap called', { mapRef: mapRef.current, google: !!window.google });
    if (!mapRef.current || !window.google) {
      console.log('Map initialization failed: missing mapRef or Google Maps API');
      return;
    }

    const center = userLocation || { lat: 35.6762, lng: 139.6503 }; // デフォルト: 東京
    console.log('Initializing map with center:', center);

    const mapInstance = new window.google.maps.Map(mapRef.current, {
      center,
      zoom: 13,
      styles: [
        {
          featureType: "poi",
          elementType: "labels",
          stylers: [{ visibility: "off" }]
        }
      ]
    });

    console.log('Map instance created:', mapInstance);
    setMap(mapInstance);

    // マーカーをクリア
    if (window.markers) {
      window.markers.forEach(marker => marker.setMap(null));
    }
    window.markers = [];

    // フィルタリングされたスポットにマーカーを追加
    console.log('Adding markers for spots:', filteredSpots.length);
    filteredSpots.forEach(spot => {
      const marker = new window.google.maps.Marker({
        position: { lat: spot.lat, lng: spot.lng },
        map: mapInstance,
        title: spot.name,
        icon: {
          url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="12" cy="12" r="10" fill="#FF6B6B" stroke="#fff" stroke-width="2"/>
              <path d="M8 12h8M12 8v8" stroke="#fff" stroke-width="2" stroke-linecap="round"/>
            </svg>
          `),
          scaledSize: new window.google.maps.Size(24, 24),
          anchor: new window.google.maps.Point(12, 12)
        }
      });

      marker.addListener("click", () => {
        handleSpotSelect(spot);
      });

      if (window.markers) {
        window.markers.push(marker);
      }
    });
  }, [filteredSpots, userLocation, handleSpotSelect]);

  // マップ更新
  useEffect(() => {
    if (map && filteredSpots.length > 0) {
      initializeMap();
    }
  }, [map, filteredSpots, initializeMap]);

  // フォーム変更ハンドラー
  const handleFormChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    setFeedbackForm(prev => ({
      ...prev,
      [name]: type === 'number' ? parseInt(value) : value
    }));
  };

  // フィードバック送信
  const handleFeedback = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSpot) return;

    try {
      const response = await fetch('/api/feedback', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          spotId: selectedSpot.id,
          ...feedbackForm
        })
      });

      if (response.ok) {
        setShowForm(false);
        setFeedbackForm({ found: undefined, rating: 0, comment: "", reportType: "" });
        // フィードバックリストを更新
        const newFeedback = await response.json();
        setFeedbacks(prev => [...prev, newFeedback]);
      }
    } catch (error) {
      console.error('フィードバック送信エラー:', error);
    }
  };

  // 写真アップロード
  const handlePhotoUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSpot) return;

    try {
      const response = await fetch('/api/photos', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          spotId: selectedSpot.id,
          ...photoForm
        })
      });

      if (response.ok) {
        setShowPhotoUpload(false);
        setPhotoForm({ url: "", caption: "" });
        // 写真リストを更新
        const newPhoto = await response.json();
        // 写真の追加が成功したことを確認
        console.log('写真が追加されました:', newPhoto);
      }
    } catch (error) {
      console.error('写真アップロードエラー:', error);
    }
  };

  // 信頼度スコア計算
  function calcTrustScore(feedbacks: Feedback[]): number {
    if (feedbacks.length === 0) return 0;
    const positive = feedbacks.filter(f => f.found).length;
    return Math.round((positive / feedbacks.length) * 100);
  }

  // スポット選択
  const handleSelectSpot = (spot: SmokingSpot) => {
    handleSpotSelect(spot);
    if (map) {
      map.setCenter({ lat: spot.lat, lng: spot.lng });
      map.setZoom(16);
    }
  };

  // ローディング状態
  if (isLoading) {
    return (
      <div className="space-y-6">
        {showSearchFilters && (
          <SearchFilters
            search={search}
            setSearch={setSearch}
            categoryFilter={categoryFilter}
            setCategoryFilter={setCategoryFilter}
            tagFilters={tagFilters}
            setTagFilters={setTagFilters}
            sortBy={sortBy}
            setSortBy={setSortBy}
            resetFilters={resetFilters}
            userLocation={userLocation}
          />
        )}
        <LoadingSkeleton type="map" />
      </div>
    );
  }

  // エラー状態
  if (error) {
    return (
      <div className="space-y-6">
        {showSearchFilters && (
          <SearchFilters
            search={search}
            setSearch={setSearch}
            categoryFilter={categoryFilter}
            setCategoryFilter={setCategoryFilter}
            tagFilters={tagFilters}
            setTagFilters={setTagFilters}
            sortBy={sortBy}
            setSortBy={setSortBy}
            resetFilters={resetFilters}
            userLocation={userLocation}
          />
        )}
        <ErrorMessage 
          message={error} 
          onRetry={() => {
            // データの再取得を試行
            getUserLocation();
          }}
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* 検索・フィルター */}
      {showSearchFilters && (
        <SearchFilters
          search={search}
          setSearch={setSearch}
          categoryFilter={categoryFilter}
          setCategoryFilter={setCategoryFilter}
          tagFilters={tagFilters}
          setTagFilters={setTagFilters}
          sortBy={sortBy}
          setSortBy={setSortBy}
          resetFilters={resetFilters}
          userLocation={userLocation}
        />
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* マップ */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-lg shadow-md p-4">
            <div ref={mapRef} className="w-full h-96 rounded-lg" />
          </div>
        </div>

        {/* スポットリスト */}
        {showSpotList && (
          <div className="lg:col-span-1">
            <SpotList
              spots={filteredSpots}
              onSelectSpot={handleSelectSpot}
              favorites={favorites}
              onToggleFavorite={toggleFavorite}
              userLocation={userLocation}
            />
          </div>
        )}
      </div>

      {/* スポット詳細モーダル */}
      {selectedSpot && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4 max-h-[90vh] overflow-y-auto">
            <div className="flex justify-between items-start mb-4">
              <h3 className="text-xl font-bold">{selectedSpot.name}</h3>
              <button
                onClick={() => setSelectedSpot(null)}
                className="text-gray-500 hover:text-gray-700"
              >
                ✕
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <p className="text-gray-600">{selectedSpot.address || ''}</p>
                <p className="text-sm text-gray-500 mt-1">{selectedSpot.description || ''}</p>
              </div>

              <div className="flex items-center space-x-2">
                <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded text-sm">
                  {selectedSpot.category}
                </span>
                {typeof selectedSpot.tags === 'string' && selectedSpot.tags.split(',').filter((tag: string) => tag.trim()).map((tag: string, index: number) => (
                  <span key={index} className="bg-gray-100 text-gray-800 px-2 py-1 rounded text-sm">
                    {tag.trim()}
                  </span>
                ))}
              </div>

              <div className="flex space-x-2">
                <button
                  onClick={() => setShowForm(true)}
                  className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
                >
                  フィードバック
                </button>
                <button
                  onClick={() => setShowPhotoUpload(true)}
                  className="bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600"
                >
                  写真追加
                </button>
              </div>

              {/* 信頼度スコア */}
              <div className="bg-gray-50 p-3 rounded">
                <p className="text-sm text-gray-600">
                  信頼度: {calcTrustScore(feedbacks)}%
                </p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* フィードバックフォーム */}
      {showForm && selectedSpot && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-bold mb-4">フィードバック</h3>
            <form onSubmit={handleFeedback} className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-2">見つかりましたか？</label>
                <select
                  name="found"
                  value={feedbackForm.found?.toString() || ""}
                  onChange={handleFormChange}
                  className="w-full p-2 border rounded"
                  required
                >
                  <option value="">選択してください</option>
                  <option value="true">はい</option>
                  <option value="false">いいえ</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">評価 (1-5)</label>
                <input
                  type="number"
                  name="rating"
                  min="1"
                  max="5"
                  value={feedbackForm.rating}
                  onChange={handleFormChange}
                  className="w-full p-2 border rounded"
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">コメント</label>
                <textarea
                  name="comment"
                  value={feedbackForm.comment}
                  onChange={handleFormChange}
                  className="w-full p-2 border rounded"
                  rows={3}
                />
              </div>

              <div className="flex space-x-2">
                <button
                  type="submit"
                  className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
                >
                  送信
                </button>
                <button
                  type="button"
                  onClick={() => setShowForm(false)}
                  className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"
                >
                  キャンセル
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 写真アップロードフォーム */}
      {showPhotoUpload && selectedSpot && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-bold mb-4">写真追加</h3>
            <form onSubmit={handlePhotoUpload} className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-2">画像URL</label>
                <input
                  type="url"
                  name="url"
                  value={photoForm.url}
                  onChange={(e) => setPhotoForm(prev => ({ ...prev, url: e.target.value }))}
                  className="w-full p-2 border rounded"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">キャプション</label>
                <input
                  type="text"
                  name="caption"
                  value={photoForm.caption}
                  onChange={(e) => setPhotoForm(prev => ({ ...prev, caption: e.target.value }))}
                  className="w-full p-2 border rounded"
                />
              </div>

              <div className="flex space-x-2">
                <button
                  type="submit"
                  className="bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600"
                >
                  追加
                </button>
                <button
                  type="button"
                  onClick={() => setShowPhotoUpload(false)}
                  className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"
                >
                  キャンセル
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default GoogleMap; 