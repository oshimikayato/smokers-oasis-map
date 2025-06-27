"use client";
import { useState } from "react";
import GoogleMap from "./components/GoogleMap";
import SpotList from "./components/SpotList";
import Header from "./components/Header";
import { useSpots } from "@/hooks/useSpots";
import "./globals.css";

export default function Home() {
  const [viewMode, setViewMode] = useState<"map" | "list">("map");
  const [selectedSpot, setSelectedSpot] = useState<any>(null);
  const [favorites, setFavorites] = useState<number[]>(() => {
    if (typeof window !== "undefined") {
      const fav = localStorage.getItem("favorites");
      return fav ? JSON.parse(fav) : [];
    }
    return [];
  });

  // カスタムフックを使用
  const {
    filteredSpots,
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

  // お気に入り切り替え
  const toggleFavorite = (spotId: number) => {
    setFavorites(favs => {
      const newFavorites = favs.includes(spotId) ? favs.filter(id => id !== spotId) : [...favs, spotId];
      if (typeof window !== "undefined") {
        localStorage.setItem("favorites", JSON.stringify(newFavorites));
      }
      return newFavorites;
    });
  };

  // スポット選択ハンドラー
  const handleSpotSelect = (spot: any) => {
    setSelectedSpot(spot);
    // モバイルでリストから選択した場合はマップ表示に切り替え
    if (window.innerWidth < 768) {
      setViewMode("map");
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-100">
      <Header />
      
      {/* モバイル用ビューモード切り替え */}
      <div className="md:hidden bg-white/80 backdrop-blur-md border-b border-white/20 sticky top-16 z-40">
        <div className="flex justify-center p-4">
          <div className="flex bg-white/60 backdrop-blur-sm rounded-2xl p-1.5 shadow-lg border border-white/30">
            <button
              onClick={() => setViewMode("map")}
              className={`px-6 py-3 rounded-xl text-sm font-semibold transition-all duration-300 flex items-center gap-2 ${
                viewMode === "map" 
                  ? "bg-white text-blue-600 shadow-lg transform scale-105" 
                  : "text-gray-600 hover:text-gray-800 hover:bg-white/50"
              }`}
              aria-label="マップ表示"
              aria-pressed={viewMode === "map"}
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-1.447-.894L15 4m0 13V4m-6 3l6-3" />
              </svg>
              マップ
            </button>
            <button
              onClick={() => setViewMode("list")}
              className={`px-6 py-3 rounded-xl text-sm font-semibold transition-all duration-300 flex items-center gap-2 ${
                viewMode === "list" 
                  ? "bg-white text-blue-600 shadow-lg transform scale-105" 
                  : "text-gray-600 hover:text-gray-800 hover:bg-white/50"
              }`}
              aria-label="リスト表示"
              aria-pressed={viewMode === "list"}
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
              </svg>
              リスト
            </button>
          </div>
        </div>
      </div>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* モバイル表示 */}
        <div className="md:hidden space-y-6">
          {viewMode === "map" ? (
            <div className="bg-white/80 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 overflow-hidden">
              <GoogleMap 
                showSearchFilters={true} 
                showSpotList={false} 
                onSpotSelect={handleSpotSelect}
                selectedSpotId={selectedSpot?.id}
              />
            </div>
          ) : (
            <div className="bg-white/80 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 overflow-hidden">
              <SpotList
                spots={filteredSpots}
                onSelectSpot={handleSpotSelect}
                favorites={favorites}
                onToggleFavorite={toggleFavorite}
                userLocation={userLocation}
                selectedSpotId={selectedSpot?.id}
              />
            </div>
          )}
        </div>

        {/* デスクトップ表示 */}
        <div className="hidden md:grid md:grid-cols-3 gap-8">
          {/* マップ表示エリア */}
          <div className="md:col-span-2">
            <div className="bg-white/80 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 overflow-hidden">
              <GoogleMap 
                showSearchFilters={true} 
                showSpotList={false} 
                onSpotSelect={handleSpotSelect}
                selectedSpotId={selectedSpot?.id}
              />
            </div>
          </div>

          {/* リスト表示エリア */}
          <div className="md:col-span-1">
            <div className="bg-white/80 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 overflow-hidden">
              <SpotList
                spots={filteredSpots}
                onSelectSpot={handleSpotSelect}
                favorites={favorites}
                onToggleFavorite={toggleFavorite}
                userLocation={userLocation}
                selectedSpotId={selectedSpot?.id}
              />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
