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
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [tagFilters, setTagFilters] = useState<string[]>([]);
  const [sortBy, setSortBy] = useState<SortOption>("name");

  // 現在地取得
  const getUserLocation = useCallback(() => {
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

  // 現在地取得
  useEffect(() => {
    getUserLocation();
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
  };
} 