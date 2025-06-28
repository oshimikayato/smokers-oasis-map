"use client";
import React, { useEffect, useRef, useState, useCallback } from "react";
import SearchFilters from "./SearchFilters";
import SpotList from "./SpotList";
import LoadingSkeleton from "./LoadingSkeleton";
import ErrorMessage from "./ErrorMessage";
import { useSpots } from "@/hooks/useSpots";
import { SmokingSpot, Feedback, FeedbackForm, PhotoForm } from "@/types";
import ShareSpot from "./ShareSpot";
import AddSpotForm from './AddSpotForm';

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
  selectedSpotId?: number | null;
}

const GoogleMap: React.FC<GoogleMapProps> = ({
  showSearchFilters = true,
  showSpotList = false,
  onSpotSelect,
  selectedSpotId
}) => {
  const mapRef = useRef<HTMLDivElement>(null);
  const [mapElement, setMapElement] = useState<HTMLDivElement | null>(null);
  const [isMapContainerReady, setIsMapContainerReady] = useState(false);
  const [currentLocationMarker, setCurrentLocationMarker] = useState<any>(null);
  const [selectedSpot, setSelectedSpot] = useState<SmokingSpot | null>(null);
  const [feedbacks, setFeedbacks] = useState<Feedback[]>([]);
  const [feedbackForm, setFeedbackForm] = useState<FeedbackForm>({ found: undefined, rating: 0, comment: "", reportType: "" });
  const [showForm, setShowForm] = useState(false);
  const [map, setMap] = useState<any>(null);
  const [showPhotoUpload, setShowPhotoUpload] = useState(false);
  const [showAddSpotForm, setShowAddSpotForm] = useState(false);
  const [photoForm, setPhotoForm] = useState<PhotoForm>({ url: "", caption: "" });

  // カスタムフックを使用
  const {
    filteredSpots,
    isLoading,
    error,
    userLocation,
    locationError,
    isLocationLoading,
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

  // スポット選択ハンドラー
  const handleSpotSelect = useCallback((spot: SmokingSpot) => {
    setSelectedSpot(spot);
    if (onSpotSelect) {
      onSpotSelect(spot);
    }
    
    // マップが存在する場合、選択されたスポットに移動
    if (map) {
      const position = { lat: spot.lat, lng: spot.lng };
      map.setCenter(position);
      map.setZoom(16);
      
      // 選択されたスポットのマーカーをハイライト
      if (window.markers) {
        window.markers.forEach(marker => {
          const markerPosition = marker.getPosition();
          if (markerPosition && 
              markerPosition.lat() === spot.lat && 
              markerPosition.lng() === spot.lng) {
            // 選択されたマーカーをハイライト
            marker.setIcon({
              url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="12" fill="#FFD700" stroke="#FF6B6B" stroke-width="3"/>
                  <path d="M8 12h8M12 8v8" stroke="#fff" stroke-width="2" stroke-linecap="round"/>
                </svg>
              `),
              scaledSize: new window.google.maps.Size(32, 32),
              anchor: new window.google.maps.Point(16, 16)
            });
          } else {
            // 他のマーカーを通常表示に戻す
            marker.setIcon({
              url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="10" fill="#FF6B6B" stroke="#fff" stroke-width="2"/>
                  <path d="M8 12h8M12 8v8" stroke="#fff" stroke-width="2" stroke-linecap="round"/>
                </svg>
              `),
              scaledSize: new window.google.maps.Size(24, 24),
              anchor: new window.google.maps.Point(12, 12)
            });
          }
        });
      }
    }
  }, [onSpotSelect, map]);

  // refの設定を確実にするためのコールバック
  const setMapRef = useCallback((node: HTMLDivElement | null) => {
    if (node) {
      setMapElement(node);
      setIsMapContainerReady(true);
    }
  }, []);

  // Google Maps APIの読み込み
  useEffect(() => {
    const loadGoogleMapsAPI = () => {
      // 既にロード済みの場合は何もしない
      if (window.google && window.google.maps) {
        return;
      }

      // 既にロード中の場合は何もしない
      const existingScript = document.querySelector('script[src*="maps.googleapis.com"]');
      if (existingScript) {
        return;
      }

      // 新しいスクリプトをロード
      const script = document.createElement("script");
      script.src = `https://maps.googleapis.com/maps/api/js?key=${process.env['NEXT_PUBLIC_GOOGLE_MAPS_API_KEY'] || ''}&libraries=places&loading=async&callback=initMap`;
      script.async = true;
      script.defer = true;
      
      // グローバルコールバック関数を定義
      (window as any).initMap = () => {
        console.log('Google Maps API script loaded successfully');
      };
      
      script.onerror = () => {
        console.error('Google Maps APIの読み込みに失敗しました');
      };
      document.head.appendChild(script);
    };

    if (typeof window !== "undefined") {
      loadGoogleMapsAPI();
    }
  }, []);

  // マップの初期化（Google Maps APIとmapElementが利用可能になったら実行）
  useEffect(() => {
    const initializeMap = () => {
      // DOM要素の存在確認を強化
      if (!mapElement || !window.google || !window.google.maps || map) {
        return;
      }

      // DOM要素が実際にレンダリングされているか確認
      if (!mapElement.offsetParent && !mapElement.getBoundingClientRect().width) {
        // 要素がまだレンダリングされていない場合は少し待つ
        setTimeout(initializeMap, 100);
        return;
      }

      const center = userLocation || { lat: 35.6762, lng: 139.6503 }; // デフォルト: 東京

      try {
        const mapInstance = new window.google.maps.Map(mapElement, {
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

        setMap(mapInstance);

        // マーカーをクリア
        if (window.markers) {
          window.markers.forEach(marker => marker.setMap(null));
        }
        window.markers = [];

        // 現在位置マーカーを追加
        if (userLocation) {
          const currentMarker = new window.google.maps.Marker({
            position: { lat: userLocation.lat, lng: userLocation.lng },
            map: mapInstance,
            title: '現在位置',
            icon: {
              url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="16" cy="16" r="14" fill="#4285F4" stroke="#fff" stroke-width="3"/>
                  <circle cx="16" cy="16" r="6" fill="#fff"/>
                  <circle cx="16" cy="16" r="3" fill="#4285F4"/>
                </svg>
              `),
              scaledSize: new window.google.maps.Size(32, 32),
              anchor: new window.google.maps.Point(16, 16)
            },
            zIndex: 1000
          });
          setCurrentLocationMarker(currentMarker);
        }

        // フィルタリングされたスポットにマーカーを追加
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
      } catch (error) {
        console.error('マップ初期化エラー:', error);
      }
    };

    // Google Maps APIとmapElementが利用可能になったら初期化
    if (window.google && window.google.maps && mapElement && !map) {
      // 少し遅延させてから初期化
      setTimeout(initializeMap, 100);
    }
  }, [mapElement, map, filteredSpots, userLocation, handleSpotSelect]);

  // マップ更新
  useEffect(() => {
    if (map && filteredSpots.length > 0) {
      // マーカーをクリア
      if (window.markers) {
        window.markers.forEach(marker => marker.setMap(null));
      }
      window.markers = [];

      // 現在位置マーカーを再表示
      if (userLocation && currentLocationMarker) {
        currentLocationMarker.setMap(map);
      }

      // 新しいマーカーを追加
      filteredSpots.forEach(spot => {
        const marker = new window.google.maps.Marker({
          position: { lat: spot.lat, lng: spot.lng },
          map: map,
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
    }
  }, [map, filteredSpots, handleSpotSelect, userLocation, currentLocationMarker]);

  // 選択されたスポットのハイライト表示
  useEffect(() => {
    if (map && window.markers && selectedSpotId) {
      const selectedSpot = filteredSpots.find(spot => spot.id === selectedSpotId);
      if (selectedSpot) {
        // マップの中心を選択されたスポットに移動
        const position = { lat: selectedSpot.lat, lng: selectedSpot.lng };
        map.setCenter(position);
        map.setZoom(16);
        
        // マーカーをハイライト
        window.markers.forEach(marker => {
          const markerPosition = marker.getPosition();
          if (markerPosition && 
              markerPosition.lat() === selectedSpot.lat && 
              markerPosition.lng() === selectedSpot.lng) {
            // 選択されたマーカーをハイライト
            marker.setIcon({
              url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="12" fill="#FFD700" stroke="#FF6B6B" stroke-width="3"/>
                  <path d="M8 12h8M12 8v8" stroke="#fff" stroke-width="2" stroke-linecap="round"/>
                </svg>
              `),
              scaledSize: new window.google.maps.Size(32, 32),
              anchor: new window.google.maps.Point(16, 16)
            });
          } else {
            // 他のマーカーを通常表示に戻す
            marker.setIcon({
              url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="10" fill="#FF6B6B" stroke="#fff" stroke-width="2"/>
                  <path d="M8 12h8M12 8v8" stroke="#fff" stroke-width="2" stroke-linecap="round"/>
                </svg>
              `),
              scaledSize: new window.google.maps.Size(24, 24),
              anchor: new window.google.maps.Point(12, 12)
            });
          }
        });
      }
    }
  }, [selectedSpotId, map, filteredSpots]);

  // 現在位置マーカーの更新
  useEffect(() => {
    if (map && userLocation) {
      // 既存の現在位置マーカーを削除
      if (currentLocationMarker) {
        currentLocationMarker.setMap(null);
      }

      // 新しい現在位置マーカーを追加
      const newCurrentMarker = new window.google.maps.Marker({
        position: { lat: userLocation.lat, lng: userLocation.lng },
        map: map,
        title: '現在位置',
        icon: {
          url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="16" cy="16" r="14" fill="#4285F4" stroke="#fff" stroke-width="3"/>
              <circle cx="16" cy="16" r="6" fill="#fff"/>
              <circle cx="16" cy="16" r="3" fill="#4285F4"/>
            </svg>
          `),
          scaledSize: new window.google.maps.Size(32, 32),
          anchor: new window.google.maps.Point(16, 16)
        },
        zIndex: 1000
      });
      setCurrentLocationMarker(newCurrentMarker);
    }
  }, [userLocation, map]);

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
    setSelectedSpot(spot);
    if (onSpotSelect) {
      onSpotSelect(spot);
    }
    
    // マップが存在する場合、選択されたスポットに移動
    if (map) {
      const position = { lat: spot.lat, lng: spot.lng };
      map.setCenter(position);
      map.setZoom(16);
      
      // 選択されたスポットのマーカーをハイライト
      if (window.markers) {
        window.markers.forEach(marker => {
          const markerPosition = marker.getPosition();
          if (markerPosition && 
              markerPosition.lat() === spot.lat && 
              markerPosition.lng() === spot.lng) {
            // 選択されたマーカーをハイライト
            marker.setIcon({
              url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="12" fill="#FFD700" stroke="#FF6B6B" stroke-width="3"/>
                  <path d="M8 12h8M12 8v8" stroke="#fff" stroke-width="2" stroke-linecap="round"/>
                </svg>
              `),
              scaledSize: new window.google.maps.Size(32, 32),
              anchor: new window.google.maps.Point(16, 16)
            });
          } else {
            // 他のマーカーを通常表示に戻す
            marker.setIcon({
              url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(`
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="10" fill="#FF6B6B" stroke="#fff" stroke-width="2"/>
                  <path d="M8 12h8M12 8v8" stroke="#fff" stroke-width="2" stroke-linecap="round"/>
                </svg>
              `),
              scaledSize: new window.google.maps.Size(24, 24),
              anchor: new window.google.maps.Point(12, 12)
            });
          }
        });
      }
    }
  };

  // スポット追加ハンドラー
  const handleSpotAdded = (newSpot: SmokingSpot) => {
    // スポットリストを更新（useSpotsフックで管理されているため、自動的に更新される）
    console.log('新しいスポットが追加されました:', newSpot);
    
    // 追加されたスポットにマップを移動
    if (map) {
      const position = { lat: newSpot.lat, lng: newSpot.lng };
      map.setCenter(position);
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
            getUserLocation={getUserLocation}
            locationError={locationError}
            isLocationLoading={isLocationLoading}
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
            getUserLocation={getUserLocation}
            locationError={locationError}
            isLocationLoading={isLocationLoading}
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
          getUserLocation={getUserLocation}
          locationError={locationError}
          isLocationLoading={isLocationLoading}
        />
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* マップ */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-lg shadow-md p-4 relative">
            <div className="relative w-full h-96 md:h-[600px] rounded-2xl overflow-hidden">
              <div
                ref={setMapRef}
                className="w-full h-full"
                style={{ minHeight: '400px' }}
              />
              
              {/* スポット追加ボタン */}
              <button
                onClick={() => setShowAddSpotForm(true)}
                className="absolute top-4 right-4 bg-blue-500 text-white p-3 rounded-full shadow-lg hover:bg-blue-600 transition-colors z-10"
                title="新しいスポットを追加"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                </svg>
              </button>

              {/* デバッグ情報 */}
              <div className="mt-2 text-xs text-gray-500">
                mapRef: {mapRef.current ? 'available' : 'null'}, 
                mapElement: {mapElement ? 'set' : 'null'}, 
                isReady: {isMapContainerReady ? 'true' : 'false'}
              </div>
            </div>
          </div>
        </div>

        {/* スポットリスト */}
        {showSpotList && (
          <div className="lg:col-span-1">
            <SpotList
              spots={filteredSpots}
              onSpotSelect={handleSelectSpot}
              selectedSpot={selectedSpot}
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
                <ShareSpot spotName={selectedSpot.name} spotId={selectedSpot.id} />
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

      {/* スポット追加フォーム */}
      {showAddSpotForm && (
        <AddSpotForm
          onClose={() => setShowAddSpotForm(false)}
          onSpotAdded={handleSpotAdded}
          userLocation={userLocation}
        />
      )}
    </div>
  );
};

export default GoogleMap; 