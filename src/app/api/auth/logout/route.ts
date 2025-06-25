import { NextResponse } from 'next/server';
import { cookies } from 'next/headers';

export async function POST() {
  try {
    const cookieStore = await cookies();
    
    // 管理者トークンを削除
    cookieStore.delete('admin-token');

    return NextResponse.json({ 
      success: true, 
      message: 'ログアウトしました' 
    });
  } catch (error) {
    console.error('Logout error:', error);
    return NextResponse.json(
      { success: false, message: 'ログアウト処理中にエラーが発生しました' },
      { status: 500 }
    );
  }
} 