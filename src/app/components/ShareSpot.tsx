"use client";
import React from "react";

interface ShareSpotProps {
  spotName: string;
  spotId: number;
}

const ShareSpot: React.FC<ShareSpotProps> = ({ spotName, spotId }) => {
  const shareUrl = `${typeof window !== "undefined" ? window.location.origin : ""}/?spot=${spotId}`;

  const handleShare = async () => {
    if (navigator.share) {
      // Web Share API
      try {
        await navigator.share({
          title: `喫煙所: ${spotName}`,
          text: `${spotName} の場所をシェアします`,
          url: shareUrl,
        });
      } catch (err) {
        // キャンセル時など
      }
    } else {
      // Fallback: URLコピー
      try {
        await navigator.clipboard.writeText(shareUrl);
        alert("URLをクリップボードにコピーしました！");
      } catch {
        alert("コピーに失敗しました");
      }
    }
  };

  return (
    <button
      onClick={handleShare}
      className="bg-blue-100 text-blue-700 px-3 py-1 rounded hover:bg-blue-200 text-sm"
      title="このスポットを共有"
    >
      共有
    </button>
  );
};

export default ShareSpot; 