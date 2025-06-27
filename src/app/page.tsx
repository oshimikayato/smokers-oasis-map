"use client";
import { useState } from "react";
import GoogleMap from "./components/GoogleMap";
import Header from "./components/Header";
import "./globals.css";

export default function Home() {
  const [viewMode, setViewMode] = useState<"map" | "list">("map");

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
              <GoogleMap showSearchFilters={true} showSpotList={false} />
            </div>
          ) : (
            <div className="space-y-6">
              <div className="bg-white/80 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 p-6">
                <div className="flex items-center gap-3 mb-6">
                  <div className="w-12 h-12 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl flex items-center justify-center">
                    <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                    </svg>
                  </div>
                  <div>
                    <h2 className="text-2xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                      喫煙所リスト
                    </h2>
                    <p className="text-gray-600 text-sm">近くの喫煙所をチェック</p>
                  </div>
                </div>
                
                <div className="space-y-4">
                  <div className="group p-6 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-2xl border border-blue-100 hover:border-blue-200 transition-all duration-300 hover:shadow-lg">
                    <div className="flex items-start justify-between mb-3">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-blue-500 rounded-xl flex items-center justify-center">
                          <span className="text-white text-lg">🚬</span>
                        </div>
                        <div>
                          <h3 className="font-bold text-gray-900">渋谷駅前喫煙所</h3>
                          <p className="text-sm text-gray-600">📍 東京都渋谷区渋谷1-1-1</p>
                        </div>
                      </div>
                      <span className="px-3 py-1 bg-blue-100 text-blue-800 text-xs font-semibold rounded-full">
                        喫煙所
                      </span>
                    </div>
                    <p className="text-sm text-gray-600 mb-3">屋内の快適な喫煙所です。分煙対応でWi-Fi完備。</p>
                    <div className="flex flex-wrap gap-2">
                      <span className="px-3 py-1 bg-white/70 text-blue-700 text-xs font-medium rounded-full border border-blue-200">
                        🏠 屋内
                      </span>
                      <span className="px-3 py-1 bg-white/70 text-green-700 text-xs font-medium rounded-full border border-green-200">
                        🚭 分煙
                      </span>
                      <span className="px-3 py-1 bg-white/70 text-purple-700 text-xs font-medium rounded-full border border-purple-200">
                        💰 無料
                      </span>
                      <span className="px-3 py-1 bg-white/70 text-orange-700 text-xs font-medium rounded-full border border-orange-200">
                        📶 Wi-Fi
                      </span>
                    </div>
                  </div>
                  
                  <div className="group p-6 bg-gradient-to-r from-green-50 to-emerald-50 rounded-2xl border border-green-100 hover:border-green-200 transition-all duration-300 hover:shadow-lg">
                    <div className="flex items-start justify-between mb-3">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-green-500 rounded-xl flex items-center justify-center">
                          <span className="text-white text-lg">🍽️</span>
                        </div>
                        <div>
                          <h3 className="font-bold text-gray-900">スターバックス 新宿店</h3>
                          <p className="text-sm text-gray-600">📍 東京都新宿区新宿3-1-1</p>
                        </div>
                      </div>
                      <span className="px-3 py-1 bg-green-100 text-green-800 text-xs font-semibold rounded-full">
                        飲食店
                      </span>
                    </div>
                    <p className="text-sm text-gray-600 mb-3">喫煙可能なカフェです。コーヒーと共にくつろぎの時間を。</p>
                    <div className="flex flex-wrap gap-2">
                      <span className="px-3 py-1 bg-white/70 text-green-700 text-xs font-medium rounded-full border border-green-200">
                        🏠 屋内
                      </span>
                      <span className="px-3 py-1 bg-white/70 text-blue-700 text-xs font-medium rounded-full border border-blue-200">
                        ☕ カフェ
                      </span>
                      <span className="px-3 py-1 bg-white/70 text-purple-700 text-xs font-medium rounded-full border border-purple-200">
                        📶 Wi-Fi
                      </span>
                    </div>
                  </div>
                  
                  <div className="text-center py-8">
                    <div className="w-16 h-16 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl mx-auto mb-4 flex items-center justify-center">
                      <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-1.447-.894L15 4m0 13V4m-6 3l6-3" />
                      </svg>
                    </div>
                    <p className="text-gray-600 mb-4">実際のデータはマップ表示でご確認ください</p>
                    <button 
                      onClick={() => setViewMode("map")}
                      className="px-6 py-3 bg-gradient-to-r from-blue-500 to-purple-600 text-white font-semibold rounded-xl hover:from-blue-600 hover:to-purple-700 transition-all duration-300 shadow-lg hover:shadow-xl transform hover:scale-105"
                    >
                      マップに戻る
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* デスクトップ表示 */}
        <div className="hidden md:grid md:grid-cols-3 gap-8">
          {/* マップ表示エリア */}
          <div className="md:col-span-2">
            <div className="bg-white/80 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 overflow-hidden">
              <GoogleMap showSearchFilters={true} showSpotList={false} />
            </div>
          </div>

          {/* リスト表示エリア */}
          <div className="md:col-span-1">
            <div className="bg-white/80 backdrop-blur-md rounded-3xl shadow-2xl border border-white/30 p-6">
              <div className="flex items-center gap-3 mb-6">
                <div className="w-12 h-12 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl flex items-center justify-center">
                  <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                  </svg>
                </div>
                <div>
                  <h2 className="text-xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                    喫煙所リスト
                  </h2>
                  <p className="text-gray-600 text-sm">デスクトップではマップとリストを並列表示</p>
                </div>
              </div>
              
              <div className="space-y-4">
                <div className="p-4 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-2xl border border-blue-100">
                  <div className="flex items-center gap-3 mb-2">
                    <div className="w-8 h-8 bg-blue-500 rounded-lg flex items-center justify-center">
                      <span className="text-white text-sm">🚬</span>
                    </div>
                    <div className="flex-1">
                      <h3 className="font-semibold text-gray-900 text-sm">渋谷駅前喫煙所</h3>
                      <p className="text-xs text-gray-600">📍 渋谷区渋谷1-1-1</p>
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-1">
                    <span className="px-2 py-1 bg-white/70 text-blue-700 text-xs rounded-full">屋内</span>
                    <span className="px-2 py-1 bg-white/70 text-green-700 text-xs rounded-full">分煙</span>
                  </div>
                </div>
                
                <div className="p-4 bg-gradient-to-r from-green-50 to-emerald-50 rounded-2xl border border-green-100">
                  <div className="flex items-center gap-3 mb-2">
                    <div className="w-8 h-8 bg-green-500 rounded-lg flex items-center justify-center">
                      <span className="text-white text-sm">🍽️</span>
                    </div>
                    <div className="flex-1">
                      <h3 className="font-semibold text-gray-900 text-sm">スターバックス 新宿店</h3>
                      <p className="text-xs text-gray-600">📍 新宿区新宿3-1-1</p>
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-1">
                    <span className="px-2 py-1 bg-white/70 text-green-700 text-xs rounded-full">カフェ</span>
                    <span className="px-2 py-1 bg-white/70 text-purple-700 text-xs rounded-full">Wi-Fi</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
