"use client";
import React, { useEffect, useRef, useState } from "react";
import SearchFilters from "./SearchFilters";
import SpotList from "./SpotList";

// 仮の喫煙所データ型
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

// フィードバックUI用の型
interface Feedback {
  id: number;
  spotId: number;
  found?: boolean;
  rating?: number;
  comment?: string;
  reportType?: string;
  createdAt: string;
}

// 写真用の型
interface Photo {
  id: number;
  spotId: number;
  url: string;
  caption?: string;
  uploadedBy?: string;
  createdAt: string;
  updatedAt: string;
}

// windowオブジェクトにmarkersプロパティを追加
declare global {
  interface Window {
    markers?: any[];
  }
}

const CATEGORY_OPTIONS = ["喫煙所", "飲食店"];
const TAG_OPTIONS = ["屋内", "屋外", "全席喫煙可", "分煙", "無料", "有料", "電源あり", "Wi-Fiあり"];

// 距離計算関数（ハバーサイン公式）
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

const GoogleMap: React.FC = () => {
  const mapRef = useRef<HTMLDivElement>(null);
  const [allSpots, setAllSpots] = useState<SmokingSpot[]>([]);
  const [filteredSpots, setFilteredSpots] = useState<SmokingSpot[]>([]);
  const [selectedSpot, setSelectedSpot] = useState<SmokingSpot | null>(null);
  const [feedbacks, setFeedbacks] = useState<Feedback[]>([]);
  const [photos, setPhotos] = useState<Photo[]>([]);
  const [feedbackForm, setFeedbackForm] = useState({ found: undefined as boolean | undefined, rating: 0, comment: "", reportType: "" });
  const [showForm, setShowForm] = useState(false);
  const [map, setMap] = useState<any>(null);
  const [userLocation, setUserLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [sortBy, setSortBy] = useState<"name" | "distance">("name");
  const [showPhotoUpload, setShowPhotoUpload] = useState(false);
  const [photoForm, setPhotoForm] = useState({ url: "", caption: "" });
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [tagFilters, setTagFilters] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [viewMode, setViewMode] = useState<"map" | "list">("map");

  // お気に入り機能
  const [favorites, setFavorites] = useState<number[]>(() => {
    if (typeof window !== "undefined") {
      const fav = localStorage.getItem("favorites");
      return fav ? JSON.parse(fav) : [];
    }
    return [];
  });

  useEffect(() => {
    if (typeof window !== "undefined") {
      localStorage.setItem("favorites", JSON.stringify(favorites));
    }
  }, [favorites]);

  const toggleFavorite = (spotId: number) => {
    setFavorites(favs => favs.includes(spotId) ? favs.filter(id => id !== spotId) : [...favs, spotId]);
  };

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

  // APIから喫煙所データを取得
  useEffect(() => {
    setIsLoading(true);
    fetch("/api/spots")
      .then((res) => res.json())
      .then((data) => {
        setAllSpots(data);
        setFilteredSpots(data);
        setIsLoading(false);
      })
      .catch((error) => {
        console.error("データ取得エラー:", error);
        setIsLoading(false);
      });
  }, []);

  // 検索・絞り込み・ソート処理
  useEffect(() => {
    let filtered = allSpots;

    // キーワード検索
    if (search.trim()) {
      const searchLower = search.toLowerCase();
      filtered = filtered.filter(spot => 
        spot.name.toLowerCase().includes(searchLower) ||
        (spot.address && spot.address.toLowerCase().includes(searchLower)) ||
        (spot.description && spot.description.toLowerCase().includes(searchLower))
      );
    }

    // カテゴリ絞り込み
    if (categoryFilter) {
      filtered = filtered.filter(spot => spot.category === categoryFilter);
    }

    // タグ絞り込み
    if (tagFilters.length > 0) {
      filtered = filtered.filter(spot => 
        tagFilters.every(tag => spot.tags.includes(tag))
      );
    }

    // ソート処理
    if (sortBy === "distance" && userLocation) {
      filtered = filtered.map(spot => ({
        ...spot,
        distance: calculateDistance(userLocation.lat, userLocation.lng, spot.lat, spot.lng)
      })).sort((a, b) => (a.distance || 0) - (b.distance || 0));
    } else {
      filtered = filtered.sort((a, b) => a.name.localeCompare(b.name));
    }

    setFilteredSpots(filtered);
  }, [allSpots, search, categoryFilter, tagFilters, sortBy, userLocation]);

  // マップ描画
  useEffect(() => {
    const g = (window as any).google;
    if (!g || !g.maps || !mapRef.current) {
      console.error("Google Maps APIが正しく読み込まれていません。APIキーやScriptの設定を確認してください。");
      return;
    }

    // 現在地取得
    const defaultCenter = { lat: 35.681236, lng: 139.767125 };
    let center = defaultCenter;
    
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          center = { lat: pos.coords.latitude, lng: pos.coords.longitude };
          initializeMap(center);
        },
        () => {
          // 位置情報取得失敗時はデフォルト
          initializeMap(center);
        }
      );
    } else {
      initializeMap(center);
    }

    function initializeMap(center: { lat: number; lng: number }) {
      const newMap = new g.maps.Map(mapRef.current, {
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

      setMap(newMap);
    }
  }, []);

  // マーカー更新
  useEffect(() => {
    if (!map || !filteredSpots.length) return;

    // 既存のマーカーをクリア
    if (window.markers) {
      window.markers.forEach((marker: any) => marker.setMap(null));
    }
    window.markers = [];

    // 新しいマーカーを作成
    filteredSpots.forEach((spot) => {
      const marker = new (window as any).google.maps.Marker({
        position: { lat: spot.lat, lng: spot.lng },
        map,
        title: spot.name,
        icon: {
          url: spot.category === "喫煙所" ? "/file.svg" : "/globe.svg",
          scaledSize: new (window as any).google.maps.Size(30, 30)
        }
      });

      const infoWindow = new (window as any).google.maps.InfoWindow({
        content: `
          <div class="p-4 max-w-sm">
            <h3 class="font-bold text-lg mb-2">${spot.name}</h3>
            <p class="text-gray-600 mb-2">${spot.category}</p>
            ${spot.address ? `<p class="text-sm text-gray-500 mb-2">${spot.address}</p>` : ''}
            ${spot.description ? `<p class="text-sm mb-2">${spot.description}</p>` : ''}
            <div class="flex flex-wrap gap-1 mb-2">
              ${spot.tags.map(tag => `<span class="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded">${tag}</span>`).join('')}
            </div>
            <button onclick="window.selectSpot(${spot.id})" class="bg-blue-600 text-white px-3 py-1 rounded text-sm">
              詳細を見る
            </button>
          </div>
        `
      });

      marker.addListener("click", () => {
        infoWindow.open(map, marker);
      });

      if (window.markers) {
        window.markers.push(marker);
      }
    });
  }, [map, filteredSpots]);

  // グローバル関数を設定
  useEffect(() => {
    (window as any).selectSpot = (spotId: number) => {
      const spot = filteredSpots.find(s => s.id === spotId);
      if (spot) {
        setSelectedSpot(spot);
        // フィードバックと写真を取得
        fetch(`/api/feedback?spotId=${spotId}`)
          .then(res => res.json())
          .then(data => setFeedbacks(data))
          .catch(console.error);
        
        fetch(`/api/photos?spotId=${spotId}`)
          .then(res => res.json())
          .then(data => setPhotos(data))
          .catch(console.error);
      }
    };
  }, [filteredSpots]);

  const handleFormChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    setFeedbackForm({ ...feedbackForm, [e.target.name]: e.target.value });
  };

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    const formData = new FormData(e.target as HTMLFormElement);
    const newSpot = {
      name: formData.get("name") as string,
      lat: parseFloat(formData.get("lat") as string),
      lng: parseFloat(formData.get("lng") as string),
      address: formData.get("address") as string,
      description: formData.get("description") as string,
      category: formData.get("category") as string,
      tags: (formData.get("tags") as string).split(",").map(t => t.trim())
    };

    try {
      const response = await fetch("/api/spots", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newSpot)
      });
      if (response.ok) {
        const addedSpot = await response.json();
        setAllSpots(prev => [...prev, addedSpot]);
        (e.target as HTMLFormElement).reset();
      }
    } catch (error) {
      console.error("スポット追加エラー:", error);
    }
  };

  const handleFeedback = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSpot) return;

    try {
      const response = await fetch("/api/feedback", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          spotId: selectedSpot.id,
          ...feedbackForm
        })
      });
      if (response.ok) {
        const newFeedback = await response.json();
        setFeedbacks(prev => [...prev, newFeedback]);
        setFeedbackForm({ found: undefined, rating: 0, comment: "", reportType: "" });
        setShowForm(false);
      }
    } catch (error) {
      console.error("フィードバック送信エラー:", error);
    }
  };

  const handlePhotoUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSpot) return;

    try {
      const response = await fetch("/api/photos", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          spotId: selectedSpot.id,
          ...photoForm
        })
      });
      if (response.ok) {
        const newPhoto = await response.json();
        setPhotos(prev => [...prev, newPhoto]);
        setPhotoForm({ url: "", caption: "" });
        setShowPhotoUpload(false);
      }
    } catch (error) {
      console.error("写真アップロードエラー:", error);
    }
  };

  const handleReport = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSpot) return;

    try {
      const response = await fetch("/api/feedback", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          spotId: selectedSpot.id,
          reportType: feedbackForm.reportType,
          comment: feedbackForm.comment
        })
      });
      if (response.ok) {
        alert("報告を送信しました。ありがとうございます。");
        setFeedbackForm({ found: undefined, rating: 0, comment: "", reportType: "" });
      }
    } catch (error) {
      console.error("報告送信エラー:", error);
    }
  };

  function calcTrustScore(feedbacks: Feedback[]): number {
    if (feedbacks.length === 0) return 0;
    const foundCount = feedbacks.filter(f => f.found === true).length;
    const totalCount = feedbacks.filter(f => f.found !== undefined).length;
    return totalCount > 0 ? Math.round((foundCount / totalCount) * 100) : 0;
  }

  const resetFilters = () => {
    setSearch("");
    setCategoryFilter("");
    setTagFilters([]);
    setSortBy("name");
  };

  const handleSelectSpot = (spot: SmokingSpot) => {
    setSelectedSpot(spot);
    // フィードバックと写真を取得
    fetch(`/api/feedback?spotId=${spot.id}`)
      .then(res => res.json())
      .then(data => setFeedbacks(data))
      .catch(console.error);
    
    fetch(`/api/photos?spotId=${spot.id}`)
      .then(res => res.json())
      .then(data => setPhotos(data))
      .catch(console.error);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* 検索・フィルター */}
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
        filteredCount={filteredSpots.length}
        totalCount={allSpots.length}
        userLocation={userLocation}
      />

      {/* ビューモード切り替え */}
      <div className="flex justify-center">
        <div className="flex bg-white rounded-lg p-1 shadow-sm">
          <button
            onClick={() => setViewMode("map")}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              viewMode === "map" 
                ? "bg-blue-600 text-white shadow-sm" 
                : "text-gray-600 hover:text-gray-800"
            }`}
          >
            <svg className="w-5 h-5 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-1.447-.894L15 4m0 13V4m-6 3l6-3" />
            </svg>
            マップ表示
          </button>
          <button
            onClick={() => setViewMode("list")}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              viewMode === "list" 
                ? "bg-blue-600 text-white shadow-sm" 
                : "text-gray-600 hover:text-gray-800"
            }`}
          >
            <svg className="w-5 h-5 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
            </svg>
            リスト表示
          </button>
        </div>
      </div>

      {/* メインコンテンツ */}
      {viewMode === "map" ? (
        <div className="bg-white rounded-lg shadow-lg overflow-hidden">
          <div ref={mapRef} className="w-full h-96" />
        </div>
      ) : (
        <SpotList
          spots={filteredSpots}
          favorites={favorites}
          onToggleFavorite={toggleFavorite}
          onSelectSpot={handleSelectSpot}
          userLocation={userLocation}
        />
      )}

      {/* スポット詳細モーダル */}
      {selectedSpot && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex justify-between items-start mb-4">
                <div>
                  <h2 className="text-2xl font-bold text-gray-900 mb-2">{selectedSpot.name}</h2>
                  <p className="text-gray-600 mb-1">{selectedSpot.category}</p>
                  {selectedSpot.address && (
                    <p className="text-sm text-gray-500 mb-2">{selectedSpot.address}</p>
                  )}
                </div>
                <button
                  onClick={() => setSelectedSpot(null)}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              {selectedSpot.description && (
                <p className="text-gray-700 mb-4">{selectedSpot.description}</p>
              )}

              <div className="flex flex-wrap gap-2 mb-4">
                {selectedSpot.tags.map(tag => (
                  <span key={tag} className="px-3 py-1 bg-blue-100 text-blue-800 text-sm rounded-full">
                    {tag}
                  </span>
                ))}
              </div>

              {/* お気に入りボタン */}
              <button
                onClick={() => toggleFavorite(selectedSpot.id)}
                className={`flex items-center gap-2 px-4 py-2 rounded-lg mb-4 transition-colors ${
                  favorites.includes(selectedSpot.id)
                    ? 'bg-red-100 text-red-600 hover:bg-red-200'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                }`}
              >
                <svg className="w-5 h-5" fill={favorites.includes(selectedSpot.id) ? "currentColor" : "none"} stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                </svg>
                {favorites.includes(selectedSpot.id) ? 'お気に入りから削除' : 'お気に入りに追加'}
              </button>

              {/* 信頼度スコア */}
              <div className="mb-4">
                <div className="flex items-center gap-2 mb-2">
                  <span className="text-sm font-medium text-gray-700">信頼度スコア:</span>
                  <span className="text-lg font-bold text-blue-600">{calcTrustScore(feedbacks)}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-blue-600 h-2 rounded-full transition-all duration-300" 
                    style={{ width: `${calcTrustScore(feedbacks)}%` }}
                  ></div>
                </div>
              </div>

              {/* 写真セクション */}
              <div className="mb-6">
                <div className="flex justify-between items-center mb-3">
                  <h3 className="text-lg font-semibold text-gray-900">写真</h3>
                  <button
                    onClick={() => setShowPhotoUpload(!showPhotoUpload)}
                    className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                  >
                    {showPhotoUpload ? 'キャンセル' : '写真を追加'}
                  </button>
                </div>

                {showPhotoUpload && (
                  <form onSubmit={handlePhotoUpload} className="mb-4 p-4 bg-gray-50 rounded-lg">
                    <div className="mb-3">
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        写真URL
                      </label>
                      <input
                        type="url"
                        name="url"
                        value={photoForm.url}
                        onChange={(e) => setPhotoForm({ ...photoForm, url: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        required
                      />
                    </div>
                    <div className="mb-3">
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        キャプション
                      </label>
                      <input
                        type="text"
                        name="caption"
                        value={photoForm.caption}
                        onChange={(e) => setPhotoForm({ ...photoForm, caption: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <button
                      type="submit"
                      className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 transition-colors"
                    >
                      アップロード
                    </button>
                  </form>
                )}

                {photos.length > 0 ? (
                  <div className="grid grid-cols-2 gap-3">
                    {photos.map(photo => (
                      <div key={photo.id} className="relative">
                        <img
                          src={photo.url}
                          alt={photo.caption || "喫煙所の写真"}
                          className="w-full h-24 object-cover rounded-lg"
                        />
                        {photo.caption && (
                          <p className="text-xs text-gray-600 mt-1">{photo.caption}</p>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-500 text-sm">まだ写真が投稿されていません</p>
                )}
              </div>

              {/* フィードバックセクション */}
              <div className="mb-6">
                <div className="flex justify-between items-center mb-3">
                  <h3 className="text-lg font-semibold text-gray-900">フィードバック</h3>
                  <button
                    onClick={() => setShowForm(!showForm)}
                    className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                  >
                    {showForm ? 'キャンセル' : 'フィードバックを送信'}
                  </button>
                </div>

                {showForm && (
                  <form onSubmit={handleFeedback} className="mb-4 p-4 bg-gray-50 rounded-lg">
                    <div className="mb-3">
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        この喫煙所を見つけましたか？
                      </label>
                      <div className="flex gap-4">
                        <label className="flex items-center">
                          <input
                            type="radio"
                            name="found"
                            value="true"
                            checked={feedbackForm.found === true}
                            onChange={(e) => setFeedbackForm({ ...feedbackForm, found: e.target.value === "true" })}
                            className="mr-2"
                          />
                          はい
                        </label>
                        <label className="flex items-center">
                          <input
                            type="radio"
                            name="found"
                            value="false"
                            checked={feedbackForm.found === false}
                            onChange={(e) => setFeedbackForm({ ...feedbackForm, found: e.target.value === "true" })}
                            className="mr-2"
                          />
                          いいえ
                        </label>
                      </div>
                    </div>
                    <div className="mb-3">
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        評価 (1-5)
                      </label>
                      <input
                        type="number"
                        name="rating"
                        min="1"
                        max="5"
                        value={feedbackForm.rating}
                        onChange={handleFormChange}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <div className="mb-3">
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        コメント
                      </label>
                      <textarea
                        name="comment"
                        value={feedbackForm.comment}
                        onChange={handleFormChange}
                        rows={3}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="この喫煙所についての感想を教えてください"
                      />
                    </div>
                    <button
                      type="submit"
                      className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 transition-colors"
                    >
                      送信
                    </button>
                  </form>
                )}

                {feedbacks.length > 0 ? (
                  <div className="space-y-3">
                    {feedbacks.map(feedback => (
                      <div key={feedback.id} className="p-3 bg-gray-50 rounded-lg">
                        <div className="flex justify-between items-start mb-2">
                          <div className="flex items-center gap-2">
                            {feedback.found !== undefined && (
                              <span className={`px-2 py-1 text-xs rounded ${
                                feedback.found 
                                  ? 'bg-green-100 text-green-800' 
                                  : 'bg-red-100 text-red-800'
                              }`}>
                                {feedback.found ? '見つかった' : '見つからなかった'}
                              </span>
                            )}
                            {feedback.rating && (
                              <div className="flex items-center gap-1">
                                {[...Array(5)].map((_, i) => (
                                  <svg
                                    key={i}
                                    className={`w-4 h-4 ${
                                      i < feedback.rating! ? 'text-yellow-400 fill-current' : 'text-gray-300'
                                    }`}
                                    fill="currentColor"
                                    viewBox="0 0 20 20"
                                  >
                                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.77.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                                  </svg>
                                ))}
                              </div>
                            )}
                          </div>
                          <span className="text-xs text-gray-500">
                            {new Date(feedback.createdAt).toLocaleDateString()}
                          </span>
                        </div>
                        {feedback.comment && (
                          <p className="text-sm text-gray-700">{feedback.comment}</p>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-500 text-sm">まだフィードバックがありません</p>
                )}
              </div>

              {/* 報告セクション */}
              <div>
                <h3 className="text-lg font-semibold text-gray-900 mb-3">問題を報告</h3>
                <form onSubmit={handleReport} className="p-4 bg-red-50 rounded-lg">
                  <div className="mb-3">
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      報告タイプ
                    </label>
                    <select
                      name="reportType"
                      value={feedbackForm.reportType}
                      onChange={handleFormChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      required
                    >
                      <option value="">選択してください</option>
                      <option value="closed">閉店・閉鎖</option>
                      <option value="moved">移転</option>
                      <option value="inappropriate">不適切な場所</option>
                      <option value="other">その他</option>
                    </select>
                  </div>
                  <div className="mb-3">
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      詳細
                    </label>
                    <textarea
                      name="comment"
                      value={feedbackForm.comment}
                      onChange={handleFormChange}
                      rows={3}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="問題の詳細を教えてください"
                      required
                    />
                  </div>
                  <button
                    type="submit"
                    className="w-full bg-red-600 text-white py-2 px-4 rounded-md hover:bg-red-700 transition-colors"
                  >
                    報告を送信
                  </button>
                </form>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default GoogleMap; 