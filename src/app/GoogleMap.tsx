"use client";
import React, { useEffect, useRef, useState } from "react";
import SearchFilters from "./components/SearchFilters";

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
        zoom: 15,
        mapTypeControl: true,
        streetViewControl: true,
        fullscreenControl: true,
        styles: [
          {
            featureType: "poi",
            elementType: "labels",
            stylers: [{ visibility: "off" }]
          }
        ]
      });
      setMap(newMap);
      
      // 地図クリックで新規登録フォーム表示
      newMap.addListener("click", (e: any) => {
        setForm(f => ({ ...f, lat: e.latLng.lat().toFixed(6), lng: e.latLng.lng().toFixed(6) }));
        setShowForm(true);
      });
    }
  }, []);

  // フィルタリングされたスポットでマーカーを更新
  useEffect(() => {
    if (!map) return;
    
    // 既存のマーカーをクリア
    const existingMarkers = window.markers;
    if (existingMarkers && existingMarkers.length > 0) {
      existingMarkers.forEach((marker: any) => marker.setMap(null));
    }
    
    const g = (window as any).google;
    if (!g || !g.maps) return;

    window.markers = [];
    
    filteredSpots.forEach((spot) => {
      const isFavorite = favorites.includes(spot.id);
      const marker = new g.maps.Marker({
        position: { lat: spot.lat, lng: spot.lng },
        map,
        title: spot.name,
        opacity: 1.0,
        icon: isFavorite
          ? {
              path: "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z",
              fillColor: '#FFD600',
              fillOpacity: 1,
              strokeColor: '#FFA000',
              strokeWeight: 2,
              scale: 1.2,
              anchor: new g.maps.Point(12, 12),
            }
          : {
              path: g.maps.SymbolPath.CIRCLE,
              fillColor: '#3B82F6',
              fillOpacity: 1,
              strokeColor: '#2563EB',
              strokeWeight: 2,
              scale: 6,
            },
      });

      marker.addListener("click", () => {
        setSelectedSpot(spot);
        // フィードバックと写真を同時に取得
        Promise.all([
          fetch(`/api/feedback?spotId=${spot.id}`).then(res => res.json()),
          fetch(`/api/photos?spotId=${spot.id}`).then(res => res.json())
        ]).then(([feedbackData, photoData]) => {
          setFeedbacks(feedbackData);
          setPhotos(photoData);
        });
        if (isFavorite) marker.setAnimation(g.maps.Animation.BOUNCE);
      });

      window.markers.push(marker);
    });

    // 検索結果がある場合は、その範囲に地図を調整
    if (filteredSpots.length > 0) {
      const bounds = new g.maps.LatLngBounds();
      filteredSpots.forEach(spot => {
        bounds.extend({ lat: spot.lat, lng: spot.lng });
      });
      map.fitBounds(bounds);
      
      // 単一のスポットの場合は適切なズームレベルを設定
      if (filteredSpots.length === 1) {
        map.setZoom(16);
      }
    }
  }, [filteredSpots, map, favorites]);

  // 新規登録フォーム
  const [form, setForm] = useState({
    name: "",
    lat: "",
    lng: "",
    address: "",
    description: "",
  });
  const [category, setCategory] = useState("喫煙所");
  const [tags, setTags] = useState<string[]>([]);

  const handleFormChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  // 新規登録時はAPIにPOST
  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.lat || !form.lng) return;
    const res = await fetch("/api/spots", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ ...form, category, tags }),
    });
    if (res.ok) {
      setForm({ name: "", lat: "", lng: "", address: "", description: "" });
      setCategory("喫煙所");
      setTags([]);
      setShowForm(false);
      // 再取得
      const spots = await fetch("/api/spots").then(r => r.json());
      setAllSpots(spots);
    }
  };

  // フィードバック送信
  const handleFeedback = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSpot) return;
    await fetch("/api/feedback", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ ...feedbackForm, spotId: selectedSpot.id }),
    });
    setFeedbackForm({ found: undefined, rating: 0, comment: "", reportType: "" });
    // 再取得
    const res = await fetch(`/api/feedback?spotId=${selectedSpot.id}`);
    setFeedbacks(await res.json());
  };

  // 写真アップロード
  const handlePhotoUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSpot || !photoForm.url) return;
    
    await fetch("/api/photos", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ 
        spotId: selectedSpot.id, 
        url: photoForm.url, 
        caption: photoForm.caption 
      }),
    });
    
    setPhotoForm({ url: "", caption: "" });
    setShowPhotoUpload(false);
    
    // 写真を再取得
    const res = await fetch(`/api/photos?spotId=${selectedSpot.id}`);
    setPhotos(await res.json());
  };

  // 通報モーダル用state
  const [showReport, setShowReport] = useState(false);
  const [reportReason, setReportReason] = useState("");
  const [reportComment, setReportComment] = useState("");

  // 通報送信
  const handleReport = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSpot) return;
    await fetch("/api/feedback", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        spotId: selectedSpot.id,
        found: undefined,
        rating: 0,
        comment: reportComment,
        reportType: reportReason,
      }),
    });
    setShowReport(false);
    setReportReason("");
    setReportComment("");
    // 再取得
    const res = await fetch(`/api/feedback?spotId=${selectedSpot.id}`);
    setFeedbacks(await res.json());
  };

  // 信頼度スコア算出関数
  function calcTrustScore(feedbacks: Feedback[]): number {
    if (!feedbacks.length) return 0;
    // 「あった」割合×50 + 平均評価×10 + コメント数×5 - 「なかった」割合×30
    const foundYes = feedbacks.filter(f => f.found === true).length;
    const foundNo = feedbacks.filter(f => f.found === false).length;
    const avgRating = feedbacks.filter(f => f.rating).reduce((a, f) => a + (f.rating || 0), 0) / (feedbacks.filter(f => f.rating).length || 1);
    const commentCount = feedbacks.filter(f => f.comment && f.comment.length > 0).length;
    const score = (foundYes / feedbacks.length) * 50 + avgRating * 10 + commentCount * 5 - (foundNo / feedbacks.length) * 30;
    return Math.max(0, Math.round(score));
  }

  // 検索条件をリセット
  const resetFilters = () => {
    setSearch("");
    setCategoryFilter("");
    setTagFilters([]);
    setSortBy("name");
  };

  return (
    <div className="space-y-6">
      {/* 検索フィルター */}
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

      {/* マップ */}
      <div className="card p-4">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-800">🗺️ 地図</h2>
          {isLoading && (
            <div className="flex items-center text-sm text-gray-600">
              <div className="spinner mr-2"></div>
              データを読み込み中...
            </div>
          )}
        </div>
        <div
          ref={mapRef}
          className="map-container w-full h-[70vh]"
        />
      </div>

      {/* 新規登録フォーム */}
      {showForm && (
        <div className="modal-overlay fixed inset-0 flex items-center justify-center z-50">
          <div className="modal-content card p-6 max-w-md w-full mx-4">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-800">📝 新規喫煙所登録</h3>
              <button
                onClick={() => setShowForm(false)}
                className="text-gray-400 hover:text-gray-600"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            
            <div className="text-sm text-gray-600 mb-4">
              地図で選択した位置: 緯度{form.lat} 経度{form.lng}
            </div>
            
            <form onSubmit={handleAdd} className="space-y-4">
              <input
                name="name"
                placeholder="名称"
                value={form.name}
                onChange={handleFormChange}
                className="input"
                required
              />
              
              <input
                name="address"
                placeholder="住所"
                value={form.address}
                onChange={handleFormChange}
                className="input"
              />
              
              <textarea
                name="description"
                placeholder="説明"
                value={form.description}
                onChange={handleFormChange}
                className="input resize-none"
                rows={3}
              />
              
              <select 
                value={category} 
                onChange={e => setCategory(e.target.value)} 
                className="input"
              >
                <option value="喫煙所">🚬 喫煙所</option>
                <option value="飲食店">🍽️ 飲食店</option>
              </select>
              
              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">🏷️ タグ</label>
                <div className="flex flex-wrap gap-2">
                  {["屋内", "屋外", "全席喫煙可", "分煙", "無料", "有料", "電源あり", "Wi-Fiあり"].map(opt => (
                    <label key={opt} className="flex items-center space-x-2">
                      <input
                        type="checkbox"
                        checked={tags.includes(opt)}
                        onChange={e => setTags(tags => e.target.checked ? [...tags, opt] : tags.filter(t => t !== opt))}
                        className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                      />
                      <span className="text-sm text-gray-700">{opt}</span>
                    </label>
                  ))}
                </div>
              </div>
              
              <div className="flex space-x-3 pt-4">
                <button type="submit" className="btn btn-primary flex-1">
                  📝 登録
                </button>
                <button 
                  type="button" 
                  className="btn btn-ghost flex-1" 
                  onClick={() => { 
                    setShowForm(false); 
                    setForm({ name: "", lat: "", lng: "", address: "", description: "" }); 
                    setCategory("喫煙所"); 
                    setTags([]); 
                  }}
                >
                  キャンセル
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 詳細・フィードバックUI */}
      {selectedSpot && (
        <div className="card p-6 animate-fade-in">
          <div className="flex justify-between items-start mb-6">
            <div className="flex-1">
              <div className="flex items-center space-x-3 mb-2">
                <h2 className="text-2xl font-bold text-gray-800">{selectedSpot.name}</h2>
                <span className={`badge ${selectedSpot.category === "喫煙所" ? "badge-primary" : "badge-secondary"}`}>
                  {selectedSpot.category === "喫煙所" ? "🚬" : "🍽️"} {selectedSpot.category}
                </span>
              </div>
              <div className="text-gray-600 mb-2">{selectedSpot.address}</div>
              <div className="text-gray-700 mb-3">{selectedSpot.description}</div>
              
              <div className="flex flex-wrap gap-2 mb-3">
                {selectedSpot.tags.map(tag => (
                  <span key={tag} className="badge badge-gray">
                    {tag}
                  </span>
                ))}
              </div>
              
              {userLocation && (
                <div className="text-sm text-blue-600 mb-2 flex items-center">
                  <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  </svg>
                  現在地からの距離: {calculateDistance(userLocation.lat, userLocation.lng, selectedSpot.lat, selectedSpot.lng).toFixed(1)}km
                </div>
              )}
              
              <div className="flex items-center space-x-4">
                <div className="flex items-center space-x-2">
                  <span className="text-sm font-medium text-gray-700">信頼度:</span>
                  <div className="flex items-center space-x-1">
                    {[...Array(5)].map((_, i) => (
                      <svg
                        key={i}
                        className={`w-4 h-4 ${
                          i < Math.floor(calcTrustScore(feedbacks) / 20)
                            ? "text-yellow-400 fill-current"
                            : "text-gray-300"
                        }`}
                        fill="currentColor"
                        viewBox="0 0 20 20"
                      >
                        <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                      </svg>
                    ))}
                  </div>
                  <span className="text-sm text-gray-600">({calcTrustScore(feedbacks)}/100)</span>
                </div>
              </div>
            </div>
            
            <button
              type="button"
              aria-label={favorites.includes(selectedSpot.id) ? "お気に入り解除" : "お気に入り追加"}
              onClick={() => toggleFavorite(selectedSpot.id)}
              className="text-yellow-400 text-4xl select-none hover:text-yellow-500 transition-colors ml-4"
              title={favorites.includes(selectedSpot.id) ? "お気に入り解除" : "お気に入り追加"}
            >
              {favorites.includes(selectedSpot.id) ? "★" : "☆"}
            </button>
          </div>

          {/* 写真ギャラリー */}
          <div className="mb-8">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold text-gray-800">📸 写真ギャラリー</h3>
              <button
                onClick={() => setShowPhotoUpload(true)}
                className="btn btn-secondary"
              >
                📷 写真を追加
              </button>
            </div>
            
            {photos.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <svg className="w-12 h-12 mx-auto mb-3 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                <p>まだ写真がありません</p>
              </div>
            ) : (
              <div className="photo-gallery">
                {photos.map(photo => (
                  <div key={photo.id} className="photo-item">
                    <img
                      src={photo.url}
                      alt={photo.caption || "喫煙所の写真"}
                      className="w-full h-32 object-cover rounded-lg cursor-pointer hover:opacity-80 transition-opacity"
                      onClick={() => window.open(photo.url, '_blank')}
                    />
                    {photo.caption && (
                      <div className="absolute bottom-0 left-0 right-0 bg-black bg-opacity-50 text-white text-xs p-2 rounded-b-lg">
                        {photo.caption}
                      </div>
                    )}
                    <div className="absolute top-2 right-2 bg-black bg-opacity-50 text-white text-xs px-2 py-1 rounded">
                      {photo.uploadedBy}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* フィードバックフォーム */}
          <div className="mb-8">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">💬 フィードバック</h3>
            <form onSubmit={handleFeedback} className="card p-4 space-y-4">
              <div className="flex gap-3 items-center">
                <span className="text-sm font-medium text-gray-700">喫煙所があった？</span>
                <button 
                  type="button" 
                  className={`btn ${feedbackForm.found===true?"btn-primary":"btn-ghost"}`} 
                  onClick={()=>setFeedbackForm(f=>({...f,found:true}))}
                >
                  ✅ あった
                </button>
                <button 
                  type="button" 
                  className={`btn ${feedbackForm.found===false?"btn-primary":"btn-ghost"}`} 
                  onClick={()=>setFeedbackForm(f=>({...f,found:false}))}
                >
                  ❌ なかった
                </button>
              </div>
              
              <div className="flex gap-3 items-center">
                <span className="text-sm font-medium text-gray-700">評価:</span>
                {[1,2,3,4,5].map(n=>(
                  <button 
                    key={n} 
                    type="button" 
                    className={`btn ${feedbackForm.rating===n?"btn-accent":"btn-ghost"}`} 
                    onClick={()=>setFeedbackForm(f=>({...f,rating:n}))}
                  >
                    {n}
                  </button>
                ))}
              </div>
              
              <textarea 
                className="input resize-none" 
                placeholder="コメント" 
                value={feedbackForm.comment} 
                onChange={e=>setFeedbackForm(f=>({...f,comment:e.target.value}))}
                rows={3}
              />
              
              <input 
                className="input" 
                placeholder="修正・報告（例: 閉鎖/移動など）" 
                value={feedbackForm.reportType} 
                onChange={e=>setFeedbackForm(f=>({...f,reportType:e.target.value}))} 
              />
              
              <button type="submit" className="btn btn-primary w-full">
                📤 フィードバック送信
              </button>
            </form>
          </div>

          {/* 最近のフィードバック */}
          <div className="mb-6">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">📋 最近のフィードバック</h3>
            {feedbacks.length === 0 ? (
              <div className="text-center py-6 text-gray-500">
                <p>まだフィードバックがありません。</p>
              </div>
            ) : (
              <div className="space-y-3">
                {feedbacks.map(f => (
                  <div key={f.id} className="card p-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <span className={`badge ${f.found === true ? "badge-secondary" : f.found === false ? "badge-danger" : "badge-gray"}`}>
                          {f.found === true ? "✅ あった" : f.found === false ? "❌ なかった" : "❓ 不明"}
                        </span>
                        <span className="text-sm text-gray-600">評価: {f.rating || "-"}</span>
                      </div>
                      <span className="text-xs text-gray-400">
                        {new Date(f.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                    {f.comment && (
                      <p className="text-sm text-gray-700 mt-2">{f.comment}</p>
                    )}
                    {f.reportType && (
                      <p className="text-xs text-red-600 mt-1">報告: {f.reportType}</p>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
          
          <div className="flex justify-end">
            <button 
              type="button" 
              className="btn btn-danger" 
              onClick={()=>setShowReport(true)}
            >
              🚨 ご情報を通報
            </button>
          </div>

          {/* 通報モーダル */}
          {showReport && (
            <div className="modal-overlay fixed inset-0 flex items-center justify-center z-50">
              <div className="modal-content card p-6 max-w-md w-full mx-4">
                <h3 className="text-lg font-semibold text-red-600 mb-4">🚨 ご情報を通報</h3>
                <form onSubmit={handleReport} className="space-y-4">
                  <select 
                    value={reportReason} 
                    onChange={e=>setReportReason(e.target.value)} 
                    className="input"
                  >
                    <option value="">通報理由を選択</option>
                    <option value="閉鎖">閉鎖</option>
                    <option value="移転">移転</option>
                    <option value="存在しない">存在しない</option>
                    <option value="その他">その他</option>
                  </select>
                  <textarea 
                    className="input resize-none" 
                    placeholder="詳細・コメント（任意）" 
                    value={reportComment} 
                    onChange={e=>setReportComment(e.target.value)}
                    rows={3}
                  />
                  <div className="flex space-x-3">
                    <button type="submit" className="btn btn-danger flex-1">
                      通報する
                    </button>
                    <button 
                      type="button" 
                      className="btn btn-ghost flex-1" 
                      onClick={()=>setShowReport(false)}
                    >
                      キャンセル
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* 写真アップロードモーダル */}
          {showPhotoUpload && (
            <div className="modal-overlay fixed inset-0 flex items-center justify-center z-50">
              <div className="modal-content card p-6 max-w-md w-full mx-4">
                <h3 className="text-lg font-semibold text-green-600 mb-4">📷 写真を追加</h3>
                <form onSubmit={handlePhotoUpload} className="space-y-4">
                  <input 
                    type="url"
                    placeholder="画像URL（例: https://example.com/image.jpg）"
                    value={photoForm.url}
                    onChange={e => setPhotoForm(f => ({ ...f, url: e.target.value }))}
                    className="input"
                    required
                  />
                  <textarea 
                    placeholder="キャプション（任意）"
                    value={photoForm.caption}
                    onChange={e => setPhotoForm(f => ({ ...f, caption: e.target.value }))}
                    className="input resize-none"
                    rows={3}
                  />
                  <div className="flex space-x-3">
                    <button type="submit" className="btn btn-secondary flex-1">
                      追加
                    </button>
                    <button 
                      type="button" 
                      className="btn btn-ghost flex-1" 
                      onClick={() => setShowPhotoUpload(false)}
                    >
                      キャンセル
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default GoogleMap;











