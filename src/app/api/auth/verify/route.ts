import { NextResponse } from 'next/server';
import { cookies } from 'next/headers';
import jwt from 'jsonwebtoken';

const JWT_SECRET = process.env['JWT_SECRET'] || 'your-secret-key';

export async function GET() {
  try {
    const cookieStore = await cookies();
    const token = cookieStore.get('admin-token');

    if (!token) {
      return NextResponse.json(
        { authenticated: false, message: 'トークンがありません' },
        { status: 401 }
      );
    }

    try {
      const decoded = jwt.verify(token.value, JWT_SECRET) as any;
      
      if (decoded.role === 'admin') {
        return NextResponse.json({ 
          authenticated: true, 
          username: decoded.username,
          role: decoded.role 
        });
      } else {
        return NextResponse.json(
          { authenticated: false, message: '管理者権限がありません' },
          { status: 403 }
        );
      }
    } catch (jwtError) {
      return NextResponse.json(
        { authenticated: false, message: '無効なトークンです' },
        { status: 401 }
      );
    }
  } catch (error) {
    console.error('Auth verification error:', error);
    return NextResponse.json(
      { authenticated: false, message: '認証確認中にエラーが発生しました' },
      { status: 500 }
    );
  }
} 