import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import GoogleMap from './GoogleMap';

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    back: jest.fn(),
  }),
}));

// Mock fetch
global.fetch = jest.fn();

// Mock Google Maps
global.google = {
  maps: {
    Map: jest.fn(),
    Marker: jest.fn(),
    InfoWindow: jest.fn(),
    LatLng: jest.fn(),
  },
} as any;

describe('GoogleMap Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders loading state initially', () => {
    render(<GoogleMap />);
    expect(screen.getByText(/読み込み中/)).toBeInTheDocument();
  });

  it('fetches spots data on mount', async () => {
    const mockSpots = [
      {
        id: 1,
        name: 'テスト喫煙所',
        lat: 35.681236,
        lng: 139.767125,
        category: '喫煙所',
        tags: ['屋内', '無料'],
      },
    ];

    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockSpots,
    });

    render(<GoogleMap />);

    await waitFor(() => {
      expect(fetch).toHaveBeenCalledWith('/api/spots');
    });
  });

  it('handles fetch error gracefully', async () => {
    (fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

    render(<GoogleMap />);

    await waitFor(() => {
      expect(screen.getByText(/読み込み中/)).not.toBeInTheDocument();
    });
  });

  it('toggles favorite status', async () => {
    const mockSpots = [
      {
        id: 1,
        name: 'テスト喫煙所',
        lat: 35.681236,
        lng: 139.767125,
        category: '喫煙所',
        tags: ['屋内', '無料'],
      },
    ];

    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockSpots,
    });

    render(<GoogleMap />);

    await waitFor(() => {
      expect(screen.getByText('テスト喫煙所')).toBeInTheDocument();
    });

    // お気に入りボタンをクリック
    const favoriteButton = screen.getByRole('button', { name: /お気に入り/ });
    fireEvent.click(favoriteButton);

    // お気に入り状態が変更されることを確認
    expect(favoriteButton).toHaveClass('text-red-500');
  });

  it('filters spots by search term', async () => {
    const mockSpots = [
      {
        id: 1,
        name: 'テスト喫煙所',
        lat: 35.681236,
        lng: 139.767125,
        category: '喫煙所',
        tags: ['屋内', '無料'],
      },
      {
        id: 2,
        name: '別の喫煙所',
        lat: 35.681236,
        lng: 139.767125,
        category: '喫煙所',
        tags: ['屋外', '無料'],
      },
    ];

    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockSpots,
    });

    render(<GoogleMap />);

    await waitFor(() => {
      expect(screen.getByText('テスト喫煙所')).toBeInTheDocument();
      expect(screen.getByText('別の喫煙所')).toBeInTheDocument();
    });

    // 検索フィールドに「テスト」と入力
    const searchInput = screen.getByPlaceholderText(/検索/);
    fireEvent.change(searchInput, { target: { value: 'テスト' } });

    // 「テスト喫煙所」のみが表示されることを確認
    expect(screen.getByText('テスト喫煙所')).toBeInTheDocument();
    expect(screen.queryByText('別の喫煙所')).not.toBeInTheDocument();
  });
}); 