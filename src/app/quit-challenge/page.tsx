"use client";
import React, { useEffect, useState } from "react";
import Link from "next/link";

interface ChallengeData {
  startDate: string;
  targetDate: string | undefined;
  motivation: string | undefined;
  dailyRecords: DailyRecord[];
}

interface DailyRecord {
  date: string;
  mood: string;
  cravings: number;
  notes: string;
  isSmokeFree: boolean;
}

const QuitChallengePage: React.FC = () => {
  const [challenge, setChallenge] = useState<ChallengeData | null>(null);
  const [motivation, setMotivation] = useState("");
  const [targetDate, setTargetDate] = useState("");
  const [showRecordForm, setShowRecordForm] = useState(false);
  const [todayRecord, setTodayRecord] = useState({
    mood: "good",
    cravings: 5,
    notes: "",
    isSmokeFree: true
  });

  // ローカルストレージからチャレンジ情報を取得
  useEffect(() => {
    const data = localStorage.getItem("quitChallenge");
    if (data) {
      setChallenge(JSON.parse(data));
    }
  }, []);

  // チャレンジ開始
  const startChallenge = (e: React.FormEvent) => {
    e.preventDefault();
    const newChallenge: ChallengeData = {
      startDate: new Date().toISOString(),
      targetDate: targetDate ? new Date(targetDate).toISOString() : undefined,
      motivation: motivation || undefined,
      dailyRecords: []
    };
    setChallenge(newChallenge);
    localStorage.setItem("quitChallenge", JSON.stringify(newChallenge));
  };

  // チャレンジリセット
  const resetChallenge = () => {
    setChallenge(null);
    localStorage.removeItem("quitChallenge");
    setMotivation("");
    setTargetDate("");
  };

  // 日数計算
  const getDays = () => {
    if (!challenge) return 0;
    const start = new Date(challenge.startDate);
    const now = new Date();
    const diff = Math.floor((now.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
    return diff + 1;
  };

  // 今日の記録を保存
  const saveTodayRecord = () => {
    if (!challenge) return;
    
    const today = new Date().toISOString().split('T')[0] as string;
    const updatedRecords = challenge.dailyRecords.filter(record => record.date !== today);
    updatedRecords.push({
      date: today,
      mood: todayRecord.mood,
      cravings: todayRecord.cravings,
      notes: todayRecord.notes,
      isSmokeFree: todayRecord.isSmokeFree
    });

    const updatedChallenge = {
      ...challenge,
      dailyRecords: updatedRecords
    };
    
    setChallenge(updatedChallenge);
    localStorage.setItem("quitChallenge", JSON.stringify(updatedChallenge));
    setShowRecordForm(false);
  };

  // 節約金額計算（1日500円想定）
  const calculateSavings = () => {
    const days = getDays();
    const dailyCost = 500; // 1日500円
    return days * dailyCost;
  };

  // 最近7日間の記録を取得
  const getRecentRecords = () => {
    if (!challenge) return [];
    const today = new Date();
    const records = [];
    for (let i = 6; i >= 0; i--) {
      const date = new Date(today);
      date.setDate(date.getDate() - i);
      const dateStr = date.toISOString().split('T')[0];
      const record = challenge.dailyRecords.find(r => r.date === dateStr);
      records.push({
        date: dateStr,
        mood: record?.mood || 'none',
        cravings: record?.cravings || 0,
        isSmokeFree: record?.isSmokeFree ?? true
      });
    }
    return records;
  };

  // 気分の色を取得
  const getMoodColor = (mood: string) => {
    switch (mood) {
      case 'great': return 'bg-green-500';
      case 'good': return 'bg-blue-500';
      case 'okay': return 'bg-yellow-500';
      case 'bad': return 'bg-orange-500';
      case 'terrible': return 'bg-red-500';
      default: return 'bg-gray-300';
    }
  };

  const recentRecords = getRecentRecords();

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-white shadow-sm border-b">
        <div className="max-w-4xl mx-auto px-4 py-4 flex items-center gap-4">
          <Link href="/" className="text-gray-600 hover:text-gray-900 transition-colors">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
          </Link>
          <h1 className="text-2xl font-bold text-gray-900">🚭 禁煙チャレンジ</h1>
        </div>
      </div>
      <div className="max-w-4xl mx-auto px-4 py-8">
        {!challenge ? (
          <form onSubmit={startChallenge} className="bg-white p-6 rounded-lg shadow-md space-y-6">
            <h2 className="text-xl font-semibold mb-2">禁煙チャレンジを始めよう！</h2>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">禁煙の動機（任意）</label>
              <input
                type="text"
                value={motivation}
                onChange={e => setMotivation(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="例: 健康のため、家族のため"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">目標日（任意）</label>
              <input
                type="date"
                value={targetDate}
                onChange={e => setTargetDate(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <button
              type="submit"
              className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 transition-colors"
            >
              チャレンジ開始
            </button>
          </form>
        ) : (
          <div className="space-y-6">
            {/* メイン情報 */}
            <div className="bg-white p-6 rounded-lg shadow-md">
              <h2 className="text-xl font-semibold mb-4">禁煙チャレンジ継続中！</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                <div className="text-center p-4 bg-blue-50 rounded-lg">
                  <div className="text-3xl font-bold text-blue-600">{getDays()}</div>
                  <div className="text-sm text-gray-600">禁煙日数</div>
                </div>
                <div className="text-center p-4 bg-green-50 rounded-lg">
                  <div className="text-3xl font-bold text-green-600">¥{calculateSavings().toLocaleString()}</div>
                  <div className="text-sm text-gray-600">節約金額</div>
                </div>
                <div className="text-center p-4 bg-purple-50 rounded-lg">
                  <div className="text-3xl font-bold text-purple-600">
                    {challenge.dailyRecords.filter(r => r.isSmokeFree).length}
                  </div>
                  <div className="text-sm text-gray-600">禁煙達成日</div>
                </div>
              </div>
              {challenge.motivation && (
                <p className="text-sm text-gray-700 mb-2">動機: {challenge.motivation}</p>
              )}
              {challenge.targetDate && (
                <p className="text-sm text-gray-700 mb-4">目標日: {new Date(challenge.targetDate).toLocaleDateString()}</p>
              )}
              <div className="flex gap-2">
                <button
                  onClick={() => setShowRecordForm(true)}
                  className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
                >
                  今日の記録を入力
                </button>
                <button
                  onClick={resetChallenge}
                  className="bg-red-100 text-red-600 px-4 py-2 rounded-md hover:bg-red-200 transition-colors"
                >
                  リセット
                </button>
              </div>
            </div>

            {/* 記録入力フォーム */}
            {showRecordForm && (
              <div className="bg-white p-6 rounded-lg shadow-md">
                <h3 className="text-lg font-semibold mb-4">今日の記録</h3>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">今日は禁煙できましたか？</label>
                    <div className="flex gap-4">
                      <label className="flex items-center">
                        <input
                          type="radio"
                          checked={todayRecord.isSmokeFree}
                          onChange={() => setTodayRecord({...todayRecord, isSmokeFree: true})}
                          className="mr-2"
                        />
                        はい
                      </label>
                      <label className="flex items-center">
                        <input
                          type="radio"
                          checked={!todayRecord.isSmokeFree}
                          onChange={() => setTodayRecord({...todayRecord, isSmokeFree: false})}
                          className="mr-2"
                        />
                        いいえ
                      </label>
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">今日の気分</label>
                    <select
                      value={todayRecord.mood}
                      onChange={(e) => setTodayRecord({...todayRecord, mood: e.target.value})}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="great">とても良い</option>
                      <option value="good">良い</option>
                      <option value="okay">普通</option>
                      <option value="bad">悪い</option>
                      <option value="terrible">とても悪い</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      喫煙欲求の強さ (1-10)
                    </label>
                    <input
                      type="range"
                      min="1"
                      max="10"
                      value={todayRecord.cravings}
                      onChange={(e) => setTodayRecord({...todayRecord, cravings: parseInt(e.target.value)})}
                      className="w-full"
                    />
                    <div className="flex justify-between text-xs text-gray-500">
                      <span>1 (弱い)</span>
                      <span>{todayRecord.cravings}</span>
                      <span>10 (強い)</span>
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">メモ</label>
                    <textarea
                      value={todayRecord.notes}
                      onChange={(e) => setTodayRecord({...todayRecord, notes: e.target.value})}
                      rows={3}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="今日の感想や気づいたこと"
                    />
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={saveTodayRecord}
                      className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
                    >
                      保存
                    </button>
                    <button
                      onClick={() => setShowRecordForm(false)}
                      className="bg-gray-300 text-gray-700 px-4 py-2 rounded-md hover:bg-gray-400 transition-colors"
                    >
                      キャンセル
                    </button>
                  </div>
                </div>
              </div>
            )}

            {/* 最近7日間のグラフ */}
            <div className="bg-white p-6 rounded-lg shadow-md">
              <h3 className="text-lg font-semibold mb-4">最近7日間の記録</h3>
              <div className="space-y-4">
                {recentRecords.map((record, index) => (
                  <div key={index} className="flex items-center gap-4 p-3 bg-gray-50 rounded-lg">
                    <div className="w-20 text-sm text-gray-600">
                      {record.date && new Date(record.date).toLocaleDateString('ja-JP', {month: 'short', day: 'numeric'})}
                    </div>
                    <div className={`w-4 h-4 rounded-full ${getMoodColor(record.mood)}`}></div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium">
                          {record.mood === 'great' ? 'とても良い' :
                           record.mood === 'good' ? '良い' :
                           record.mood === 'okay' ? '普通' :
                           record.mood === 'bad' ? '悪い' :
                           record.mood === 'terrible' ? 'とても悪い' : '記録なし'}
                        </span>
                        {record.cravings > 0 && (
                          <span className="text-xs text-gray-500">
                            欲求: {record.cravings}/10
                          </span>
                        )}
                      </div>
                    </div>
                    <div className={`px-2 py-1 rounded text-xs ${
                      record.isSmokeFree ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                    }`}>
                      {record.isSmokeFree ? '禁煙達成' : '喫煙'}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* 記録履歴 */}
            {challenge.dailyRecords.length > 0 && (
              <div className="bg-white p-6 rounded-lg shadow-md">
                <h3 className="text-lg font-semibold mb-4">記録履歴</h3>
                <div className="space-y-3">
                  {challenge.dailyRecords
                    .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
                    .slice(0, 10)
                    .map((record, index) => (
                    <div key={index} className="p-3 border border-gray-200 rounded-lg">
                      <div className="flex justify-between items-start mb-2">
                        <span className="font-medium">{new Date(record.date).toLocaleDateString()}</span>
                        <span className={`px-2 py-1 rounded text-xs ${
                          record.isSmokeFree ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                        }`}>
                          {record.isSmokeFree ? '禁煙達成' : '喫煙'}
                        </span>
                      </div>
                      <div className="text-sm text-gray-600 space-y-1">
                        <div>気分: {
                          record.mood === 'great' ? 'とても良い' :
                          record.mood === 'good' ? '良い' :
                          record.mood === 'okay' ? '普通' :
                          record.mood === 'bad' ? '悪い' :
                          record.mood === 'terrible' ? 'とても悪い' : '記録なし'
                        }</div>
                        {record.cravings > 0 && <div>喫煙欲求: {record.cravings}/10</div>}
                        {record.notes && <div>メモ: {record.notes}</div>}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default QuitChallengePage; 