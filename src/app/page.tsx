"use client";
import { useState } from "react";
import GoogleMap from "./components/GoogleMap";
import Header from "./components/Header";
import "./globals.css";

export default function Home() {
  const [viewMode, setViewMode] = useState<"map" | "list">("map");

  return (
    <div className="min-h-screen bg-gray-50">
      <Header />
      
      {/* モバイル用ビューモード切り替え */}
      <div className="md:hidden bg-white border-b border-gray-200">
        <div className="flex justify-center p-2">
          <div className="flex bg-gray-100 rounded-lg p-1">
            <button
              onClick={() => setViewMode("map")}
              className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                viewMode === "map" 
                  ? "bg-white text-blue-600 shadow-sm" 
                  : "text-gray-600 hover:text-gray-800"
              }`}
              aria-label="マップ表示"
            >
              <svg className="w-4 h-4 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-1.447-.894L15 4m0 13V4m-6 3l6-3" />
              </svg>
              マップ
            </button>
            <button
              onClick={() => setViewMode("list")}
              className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                viewMode === "list" 
                  ? "bg-white text-blue-600 shadow-sm" 
                  : "text-gray-600 hover:text-gray-800"
              }`}
              aria-label="リスト表示"
            >
              <svg className="w-4 h-4 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
              </svg>
              リスト
            </button>
          </div>
        </div>
      </div>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* マップ表示エリア */}
          <div className={`${viewMode === "map" ? "block" : "hidden"} lg:block lg:col-span-2`}>
            <div className="bg-white rounded-lg shadow-md p-4">
              <GoogleMap />
            </div>
          </div>

          {/* リスト表示エリア */}
          <div className={`${viewMode === "list" ? "block" : "hidden"} lg:block lg:col-span-1`}>
            <div className="bg-white rounded-lg shadow-md p-4">
              <h2 className="text-lg font-semibold mb-4">喫煙所リスト</h2>
              <p className="text-gray-600">リスト表示機能は現在開発中です...</p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
