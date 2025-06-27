import { useState, useEffect, useMemo, useCallback } from 'react';
import { SmokingSpot, UserLocation, SortOption } from '@/types';

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

export function useSpots() {
  const [allSpots, setAllSpots] = useState<SmokingSpot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [userLocation, setUserLocation] = useState<UserLocation | null>(null);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [isLocationLoading, setIsLocationLoading] = useState(false);
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [tagFilters, setTagFilters] = useState<string[]>([]);
  const [sortBy, setSortBy] = useState<SortOption>("name");

  // 現在地取得（改善版）
  const getUserLocation = useCallback(() => {
    if (!navigator.geolocation) {
      setLocationError("お使いのブラウザは位置情報をサポートしていません");
      return;
    }

    setIsLocationLoading(true);
    setLocationError(null);

    const options = {
      enableHighAccuracy: false, // 高精度を無効にしてタイムアウトを防ぐ
      timeout: 30000, // 30秒に延長
      maximumAge: 600000 // 10分以内のキャッシュされた位置情報を使用
    };

    const successCallback = (position: GeolocationPosition) => {
      const location = { 
        lat: position.coords.latitude, 
        lng: position.coords.longitude,
        accuracy: position.coords.accuracy, // 精度（メートル）
        timestamp: position.timestamp
      };
      setUserLocation(location);
      setLocationError(null);
      setIsLocationLoading(false);
      console.log('現在地を取得しました:', location);
    };

    const errorCallback = (error: GeolocationPositionError) => {
      setIsLocationLoading(false);
      let errorMessage = "位置情報の取得に失敗しました";
      
      switch (error.code) {
        case error.PERMISSION_DENIED:
          errorMessage = "位置情報の使用が許可されていません。ブラウザの設定で位置情報を許可してください。";
          break;
        case error.POSITION_UNAVAILABLE:
          errorMessage = "位置情報を取得できませんでした。";
          break;
        case error.TIMEOUT:
          errorMessage = "位置情報の取得がタイムアウトしました。ネットワーク環境を確認してください。";
          break;
        default:
          errorMessage = "位置情報の取得中にエラーが発生しました。";
      }
      
      setLocationError(errorMessage);
      console.error('位置情報取得エラー:', error);
    };

    navigator.geolocation.getCurrentPosition(successCallback, errorCallback, options);
  }, []);

  // 位置情報の監視（リアルタイム更新）
  const startLocationWatching = useCallback(() => {
    if (!navigator.geolocation) return;

    const options = {
      enableHighAccuracy: false, // 高精度を無効にしてタイムアウトを防ぐ
      timeout: 30000, // 30秒に延長
      maximumAge: 300000 // 5分以内のキャッシュ
    };

    const watchId = navigator.geolocation.watchPosition(
      (position) => {
        const location = { 
          lat: position.coords.latitude, 
          lng: position.coords.longitude,
          accuracy: position.coords.accuracy,
          timestamp: position.timestamp
        };
        setUserLocation(location);
        setLocationError(null);
      },
      (error) => {
        console.error('位置情報監視エラー:', error);
        // 監視エラーは致命的ではないので、エラーメッセージは設定しない
      },
      options
    );

    return watchId;
  }, []);

  // 現在地取得の初期化
  useEffect(() => {
    getUserLocation();
    
    // 位置情報の監視は無効化（タイムアウトエラーを防ぐため）
    // const watchId = startLocationWatching();
    
    // return () => {
    //   if (watchId) {
    //     navigator.geolocation.clearWatch(watchId);
    //   }
    // };
  }, [getUserLocation]);

  // APIから喫煙所データを取得
  useEffect(() => {
    const fetchSpots = async () => {
      try {
        setIsLoading(true);
        setError(null);
        
        const response = await fetch('/api/spots');
        if (!response.ok) {
          throw new Error('データの取得に失敗しました');
        }
        
        const data = await response.json();
        // APIレスポンスの型を正しく変換
        const spots: SmokingSpot[] = data.map((spot: any) => ({
          ...spot,
          address: spot.address ?? '',
          description: spot.description ?? '',
          tags: typeof spot.tags === 'string' ? spot.tags : Array.isArray(spot.tags) ? spot.tags.join(',') : ''
        }));
        setAllSpots(spots);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'エラーが発生しました');
        console.error('喫煙所データ取得エラー:', err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchSpots();
  }, []);

  // フィルタリングとソート処理
  const filteredSpots = useMemo(() => {
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

    return filtered;
  }, [allSpots, search, categoryFilter, tagFilters, sortBy, userLocation]);

  // フィルターリセット
  const resetFilters = useCallback(() => {
    setSearch("");
    setCategoryFilter("");
    setTagFilters([]);
    setSortBy("name");
  }, []);

  return {
    allSpots,
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
    getUserLocation,
    startLocationWatching
  };
} 