import { NextResponse, NextRequest } from "next/server";
import { prisma } from "@/lib/prisma";

// テスト用のフォールバックデータ
const fallbackSpots = [
  {
    id: 1,
    name: "渋谷駅東口喫煙所",
    lat: 35.658034,
    lng: 139.701636,
    address: "東京都渋谷区渋谷1-1-1",
    description: "渋谷駅東口の屋外喫煙所。24時間利用可能。",
    category: "喫煙所",
    tags: "屋外,無料,24時間"
  },
  {
    id: 2,
    name: "新宿駅南口喫煙所",
    lat: 35.689521,
    lng: 139.700804,
    address: "東京都新宿区新宿3-1-1",
    description: "新宿駅南口の屋内喫煙所。空調完備。",
    category: "喫煙所",
    tags: "屋内,無料,空調完備"
  },
  {
    id: 3,
    name: "スターバックス 渋谷店",
    lat: 35.659027,
    lng: 139.703599,
    address: "東京都渋谷区渋谷2-21-1",
    description: "渋谷のスターバックス。喫煙席あり。",
    category: "飲食店",
    tags: "屋内,分煙,有料,Wi-Fiあり,電源あり"
  },
  {
    id: 4,
    name: "タリーズコーヒー 新宿店",
    lat: 35.690921,
    lng: 139.700304,
    address: "東京都新宿区新宿2-2-1",
    description: "新宿のタリーズ。喫煙可能な席を完備。",
    category: "飲食店",
    tags: "屋内,分煙,有料,Wi-Fiあり"
  },
  {
    id: 5,
    name: "原宿駅前喫煙所",
    lat: 35.670168,
    lng: 139.701636,
    address: "東京都渋谷区神宮前1-1-1",
    description: "原宿駅前の屋外喫煙所。",
    category: "喫煙所",
    tags: "屋外,無料"
  },
  {
    id: 6,
    name: "表参道喫煙所",
    lat: 35.665428,
    lng: 139.712356,
    address: "東京都渋谷区神宮前4-1-1",
    description: "表参道の屋外喫煙所。",
    category: "喫煙所",
    tags: "屋外,無料"
  }
];

export async function GET(request: Request) {
  try {
    // クエリパラメータでカテゴリ・タグ絞り込み
    const { searchParams } = new URL(request.url);
    const category = searchParams.get("category");
    const tag = searchParams.get("tag");
    const keyword = searchParams.get("q");

    // データベースが利用可能な場合はDBから取得
    try {
      const where: any = {};
      if (category) where.category = category;
      if (tag) where.tags = { contains: tag };
      if (keyword) {
        where.OR = [
          { name: { contains: keyword } },
          { address: { contains: keyword } },
          { description: { contains: keyword } },
        ];
      }

      const spots = await prisma.smokingSpot.findMany({ where });
      return NextResponse.json(spots);
    } catch (dbError) {
      console.log("データベースエラー、フォールバックデータを使用:", dbError);
      
      // フォールバックデータでフィルタリング
      let filteredSpots = fallbackSpots;
      
      if (category) {
        filteredSpots = filteredSpots.filter(spot => spot.category === category);
      }
      
      if (tag) {
        filteredSpots = filteredSpots.filter(spot => spot.tags.includes(tag));
      }
      
      if (keyword) {
        const lowerKeyword = keyword.toLowerCase();
        filteredSpots = filteredSpots.filter(spot => 
          spot.name.toLowerCase().includes(lowerKeyword) ||
          (spot.address && spot.address.toLowerCase().includes(lowerKeyword)) ||
          (spot.description && spot.description.toLowerCase().includes(lowerKeyword))
        );
      }
      
      return NextResponse.json(filteredSpots);
    }
  } catch (error) {
    console.error("API エラー:", error);
    return NextResponse.json({ error: "サーバーエラーが発生しました" }, { status: 500 });
  }
}

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { name, address, latitude, longitude, category, description, tags } = body;

    // 必須フィールドのバリデーション
    if (!name || !address || !latitude || !longitude || !category) {
      return NextResponse.json(
        { error: '必須フィールドが不足しています' },
        { status: 400 }
      );
    }

    // 座標のバリデーション
    if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180) {
      return NextResponse.json(
        { error: '無効な座標です' },
        { status: 400 }
      );
    }

    // 新しいスポットを作成
    const newSpot = await prisma.smokingSpot.create({
      data: {
        name,
        address,
        latitude,
        longitude,
        category,
        description: description || '',
        tags: tags || '',
        createdAt: new Date(),
        updatedAt: new Date()
      }
    });

    return NextResponse.json(newSpot, { status: 201 });
  } catch (error) {
    console.error('スポット追加エラー:', error);
    return NextResponse.json(
      { error: 'スポットの追加に失敗しました' },
      { status: 500 }
    );
  }
}
