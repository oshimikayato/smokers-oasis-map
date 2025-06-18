import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

async function main() {
  // 既存のデータをクリア
  await prisma.photo.deleteMany();
  await prisma.feedback.deleteMany();
  await prisma.smokingSpot.deleteMany();

  // テストデータを追加
  const testSpots = [
    {
      name: "渋谷駅東口喫煙所",
      lat: 35.658034,
      lng: 139.701636,
      address: "東京都渋谷区渋谷1-1-1",
      description: "渋谷駅東口の屋外喫煙所。24時間利用可能。",
      category: "喫煙所",
      tags: ["屋外", "無料", "24時間"]
    },
    {
      name: "新宿駅南口喫煙所",
      lat: 35.689521,
      lng: 139.700804,
      address: "東京都新宿区新宿3-1-1",
      description: "新宿駅南口の屋内喫煙所。空調完備。",
      category: "喫煙所",
      tags: ["屋内", "無料", "空調完備"]
    },
    {
      name: "スターバックス 渋谷店",
      lat: 35.659027,
      lng: 139.703599,
      address: "東京都渋谷区渋谷2-21-1",
      description: "渋谷のスターバックス。喫煙席あり。",
      category: "飲食店",
      tags: ["屋内", "分煙", "有料", "Wi-Fiあり", "電源あり"]
    },
    {
      name: "タリーズコーヒー 新宿店",
      lat: 35.690921,
      lng: 139.700304,
      address: "東京都新宿区新宿2-2-1",
      description: "新宿のタリーズ。喫煙可能な席を完備。",
      category: "飲食店",
      tags: ["屋内", "分煙", "有料", "Wi-Fiあり"]
    },
    {
      name: "原宿駅前喫煙所",
      lat: 35.670168,
      lng: 139.701636,
      address: "東京都渋谷区神宮前1-1-1",
      description: "原宿駅前の屋外喫煙所。",
      category: "喫煙所",
      tags: ["屋外", "無料"]
    },
    {
      name: "表参道喫煙所",
      lat: 35.665428,
      lng: 139.712356,
      address: "東京都渋谷区神宮前4-1-1",
      description: "表参道の屋外喫煙所。",
      category: "喫煙所",
      tags: ["屋外", "無料"]
    }
  ];

  for (const spot of testSpots) {
    await prisma.smokingSpot.create({
      data: spot
    });
  }

  // サンプルフィードバックも追加
  const spots = await prisma.smokingSpot.findMany();
  
  for (const spot of spots) {
    await prisma.feedback.createMany({
      data: [
        {
          spotId: spot.id,
          found: true,
          rating: 4,
          comment: "きれいで使いやすい喫煙所でした",
          reportType: null
        },
        {
          spotId: spot.id,
          found: true,
          rating: 3,
          comment: "普通の喫煙所です",
          reportType: null
        }
      ]
    });
  }

  // サンプル写真データを追加
  const samplePhotos = [
    {
      spotId: 1, // 渋谷駅東口喫煙所
      url: "https://images.unsplash.com/photo-1518837695005-2083093ee35b?w=400&h=300&fit=crop",
      caption: "渋谷駅東口の喫煙所の様子",
      uploadedBy: "ユーザー1"
    },
    {
      spotId: 1,
      url: "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=300&fit=crop",
      caption: "夜間の様子",
      uploadedBy: "ユーザー2"
    },
    {
      spotId: 3, // スターバックス 渋谷店
      url: "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=400&h=300&fit=crop",
      caption: "スターバックスの喫煙席",
      uploadedBy: "ユーザー3"
    },
    {
      spotId: 2, // 新宿駅南口喫煙所
      url: "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=400&h=300&fit=crop",
      caption: "新宿駅南口の屋内喫煙所",
      uploadedBy: "ユーザー4"
    }
  ];

  for (const photo of samplePhotos) {
    await prisma.photo.create({
      data: photo
    });
  }

  console.log('テストデータの追加が完了しました！');
  console.log(`追加された喫煙所: ${spots.length}件`);
  console.log(`追加された写真: ${samplePhotos.length}件`);
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  }); 