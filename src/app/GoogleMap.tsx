"use client";
import React, { useEffect, useRef, useState } from "react";

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

const CATEGORY_OPTIONS = ["喫煙所", "飲食店"];
const TAG_OPTIONS = ["屋内", "屋外", "全席喫煙可", "分煙", "無料", "有料", "電源あり", "Wi-Fiあり"];

const GoogleMap: React.FC = () => {
  const mapRef = useRef<HTMLDivElement>(null);
  const [spots, setSpots] = useState<SmokingSpot[]>([]);
  const [infoWindow, setInfoWindow] = useState<any>(null);
  const [search, setSearch] = useState("");
  const [filtered, setFiltered] = useState<SmokingSpot[]>([]);
  const [category, setCategory] = useState("喫煙所");
  const [tags, setTags] = useState<string[]>([]);
  const [tagFilter, setTagFilter] = useState<string>("");
  const [categoryFilter, setCategoryFilter] = useState("");

  const [selectedSpot, setSelectedSpot] = useState<SmokingSpot | null>(null);
  const [feedbacks, setFeedbacks] = useState<Feedback[]>([]);
  const [feedbackForm, setFeedbackForm] = useState({ found: undefined as boolean | undefined, rating: 0, comment: "", reportType: "" });
  const [tagFilters, setTagFilters] = useState<string[]>([]);
  const [showForm, setShowForm] = useState(false);

  // APIから喫煙所データを取得
  useEffect(() => {
    fetch("/api/spots")
      .then((res) => res.json())
      .then((data) => {
        setSpots(data);
        setFiltered(data);
      });
  }, []);

  // 検索・絞り込み
  useEffect(() => {
    const params = new URLSearchParams();
    if (search) params.append("q", search);
    if (categoryFilter) params.append("category", categoryFilter);
    tagFilters.forEach((tag: string) => params.append("tag", tag));
    fetch(`/api/spots?${params.toString()}`)
      .then((res) => res.json())
      .then((data) => setSpots(data));
  }, [search, categoryFilter, tagFilters]);

  // マップ描画
  const [registerMode, setRegisterMode] = useState(false);
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
          const map = new g.maps.Map(mapRef.current, {
            center,
            zoom: 15,
          });
          addMarkers(map);
        },
        () => {
          // 位置情報取得失敗時はデフォルト
          const map = new g.maps.Map(mapRef.current, {
            center,
            zoom: 15,
          });
          addMarkers(map);
        }
      );
    } else {
      const map = new g.maps.Map(mapRef.current, {
        center,
        zoom: 15,
      });
      addMarkers(map);
    }
    function addMarkers(map: any) {
      filtered.forEach((spot) => {
        // お気に入りなら星型アイコン、通常は青丸
        const isFavorite = favorites.includes(spot.id);
        const marker = new g.maps.Marker({
          position: { lat: spot.lat, lng: spot.lng },
          map,
          title: spot.name,
          opacity: 1.0,
          icon: isFavorite
            ? {
                path: "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z", // 星型SVGパス
                fillColor: '#FFD600',
                fillOpacity: 1,
                strokeColor: '#FFA000',
                strokeWeight: 2,
                scale: 1.2,
                anchor: new g.maps.Point(12, 12),
              }
            : {
                path: g.maps.SymbolPath.CIRCLE,
                fillColor: '#1976D2',
                fillOpacity: 1,
                strokeColor: '#1565C0',
                strokeWeight: 2,
                scale: 6,
              },
        });
        marker.addListener("click", () => {
          setSelectedSpot(spot);
          fetch(`/api/feedback?spotId=${spot.id}`)
            .then(res => res.json())
            .then(setFeedbacks);
          if (isFavorite) marker.setAnimation(g.maps.Animation.BOUNCE);
        });
      });
      map.addListener("click", (e: any) => {
        setForm(f => ({ ...f, lat: e.latLng.lat().toFixed(6), lng: e.latLng.lng().toFixed(6) }));
        setShowForm(true);
      });
    }
  }, [filtered]);

  // 新規登録フォーム
  const [form, setForm] = useState({
    name: "",
    lat: "",
    lng: "",
    address: "",
    description: "",
  });
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
      setSpots(spots);
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

  return (
    <div>
      <div
        ref={mapRef}
        style={{ width: "100%", height: "80vh", maxWidth: "1400px", margin: "0 auto", backgroundColor: "#fff" }}
      />
      <div className="my-4 flex flex-col gap-2 max-w-xl mx-auto bg-white bg-opacity-90 p-4 rounded shadow">
        <input
          type="text"
          placeholder="キーワード検索（名称・住所・説明）"
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="border p-1 rounded text-black bg-white placeholder-gray-700"
        />
        <select value={categoryFilter} onChange={e => setCategoryFilter(e.target.value)} className="border p-1 rounded text-black bg-white">
          <option value="">カテゴリ絞り込み（全て）</option>
          {CATEGORY_OPTIONS.map(opt => <option key={opt} value={opt}>{opt}</option>)}
        </select>
        <div className="flex flex-wrap gap-2">
          {TAG_OPTIONS.map(opt => (
            <label key={opt} className="flex items-center gap-1 text-black">
              <input
                type="checkbox"
                checked={tagFilters.includes(opt)}
                onChange={e => setTagFilters((tags: string[]) => e.target.checked ? [...tags, opt] : tags.filter((t: string) => t !== opt))}
              />
              {opt}
            </label>
          ))}
        </div>
      </div>
      {/* 新規登録フォームはregisterMode時のみ表示 */}
      {showForm && (
        <form onSubmit={handleAdd} className="fixed inset-0 bg-black bg-opacity-30 flex items-center justify-center z-50">
          <div className="flex flex-col gap-2 border p-4 rounded bg-white max-w-xs w-full shadow">
            <div className="text-black font-bold mb-2">新規喫煙所登録</div>
            <div className="text-sm text-black">地図で選択した位置: 緯度{form.lat} 経度{form.lng}</div>
            <select value={category} onChange={e => setCategory(e.target.value)} className="border p-1 rounded text-black bg-white">
              {CATEGORY_OPTIONS.map(opt => <option key={opt} value={opt}>{opt}</option>)}
            </select>
            <div className="flex flex-wrap gap-2">
              {TAG_OPTIONS.map(opt => (
                <label key={opt} className="flex items-center gap-1 text-black">
                  <input
                    type="checkbox"
                    checked={tags.includes(opt)}
                    onChange={e => setTags(tags => e.target.checked ? [...tags, opt] : tags.filter(t => t !== opt))}
                  />
                  {opt}
                </label>
              ))}
            </div>
            <div className="flex gap-2 mt-2">
              <button type="submit" className="bg-blue-500 text-white rounded px-2 py-1 flex-1">登録</button>
              <button type="button" className="bg-gray-300 text-black rounded px-2 py-1 flex-1" onClick={() => { setShowForm(false); setForm({ name: "", lat: "", lng: "", address: "", description: "" }); setCategory("喫煙所"); setTags([]); }}>キャンセル</button>
            </div>
          </div>
        </form>
      )}
      {/* マップの下に詳細・フィードバックUIを表示 */}
      {selectedSpot && (
        <div className="border rounded p-4 my-4 bg-white max-w-xl mx-auto">
          <div className="flex justify-between items-center">
            <h2 className="font-bold text-lg mb-2 text-black">{selectedSpot.name}</h2>
            <button
              type="button"
              aria-label={favorites.includes(selectedSpot.id) ? "お気に入り解除" : "お気に入り追加"}
              onClick={() => toggleFavorite(selectedSpot.id)}
              className="ml-2 text-yellow-400 text-2xl select-none"
              title={favorites.includes(selectedSpot.id) ? "お気に入り解除" : "お気に入り追加"}
            >
              {favorites.includes(selectedSpot.id) ? "★" : "☆"}
            </button>
          </div>
          <div className="mb-1 text-black">{selectedSpot.address}</div>
          <div className="mb-2 text-black">{selectedSpot.description}</div>
          <div className="mb-2 text-black">カテゴリ: {selectedSpot.category} / タグ: {selectedSpot.tags.join(", ")}</div>
          <div className="mb-2 font-bold text-green-700">信頼度スコア: {calcTrustScore(feedbacks)} / 100</div>
          <form onSubmit={handleFeedback} className="flex flex-col gap-2 mb-2">
            <div className="flex gap-2 items-center text-black">
              <span>喫煙所があった？</span>
              <button type="button" className={`px-2 py-1 rounded ${feedbackForm.found===true?"bg-blue-500 text-white":"bg-gray-200 text-black"}`} onClick={()=>setFeedbackForm(f=>({...f,found:true}))}>あった</button>
              <button type="button" className={`px-2 py-1 rounded ${feedbackForm.found===false?"bg-blue-500 text-white":"bg-gray-200 text-black"}`} onClick={()=>setFeedbackForm(f=>({...f,found:false}))}>なかった</button>
            </div>
            <div className="flex gap-2 items-center text-black">
              <span>評価:</span>
              {[1,2,3,4,5].map(n=>(
                <button key={n} type="button" className={`px-2 py-1 rounded ${feedbackForm.rating===n?"bg-yellow-400 text-black":"bg-gray-200 text-black"}`} onClick={()=>setFeedbackForm(f=>({...f,rating:n}))}>{n}</button>
              ))}
            </div>
            <textarea className="border p-1 rounded text-black" placeholder="コメント" value={feedbackForm.comment} onChange={e=>setFeedbackForm(f=>({...f,comment:e.target.value}))} />
            <input className="border p-1 rounded text-black" placeholder="修正・報告（例: 閉鎖/移動など）" value={feedbackForm.reportType} onChange={e=>setFeedbackForm(f=>({...f,reportType:e.target.value}))} />
            <button type="submit" className="bg-blue-500 text-white rounded px-2 py-1">フィードバック送信</button>
          </form>
          <div className="mt-2">
            <div className="font-bold text-black">最近のフィードバック</div>
            {feedbacks.length===0 && <div className="text-black">まだフィードバックがありません。</div>}
            {feedbacks.map(f=>(
              <div key={f.id} className="border-b py-1 text-sm text-black">
                <span>{f.found===true?"✅あった":f.found===false?"❌なかった":""}</span>
                <span className="ml-2">評価: {f.rating||"-"}</span>
                <span className="ml-2">{f.comment}</span>
                <span className="ml-2 text-gray-500">{f.reportType}</span>
              </div>
            ))}
          </div>
          <div className="mt-4 flex justify-end">
            <button type="button" className="text-xs text-red-600 underline" onClick={()=>setShowReport(true)}>
              ご情報を通報
            </button>
          </div>
          {showReport && (
            <form onSubmit={handleReport} className="fixed inset-0 bg-black bg-opacity-40 flex items-center justify-center z-50">
              <div className="bg-white p-4 rounded shadow flex flex-col gap-2 max-w-xs w-full">
                <div className="font-bold text-red-600 mb-2">ご情報を通報</div>
                <select value={reportReason} onChange={e=>setReportReason(e.target.value)} className="border p-1 rounded text-black bg-white">
                  <option value="">通報理由を選択</option>
                  <option value="閉鎖">閉鎖</option>
                  <option value="移転">移転</option>
                  <option value="存在しない">存在しない</option>
                  <option value="その他">その他</option>
                </select>
                <textarea className="border p-1 rounded text-black" placeholder="詳細・コメント（任意）" value={reportComment} onChange={e=>setReportComment(e.target.value)} />
                <div className="flex gap-2 mt-2">
                  <button type="submit" className="bg-red-500 text-white rounded px-2 py-1 flex-1">通報する</button>
                  <button type="button" className="bg-gray-300 text-black rounded px-2 py-1 flex-1" onClick={()=>setShowReport(false)}>キャンセル</button>
                </div>
              </div>
            </form>
          )}
        </div>
      )}
    </div>
  );
};

export default GoogleMap;











