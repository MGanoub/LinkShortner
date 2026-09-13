import { useState, useEffect } from 'react';
import './App.css';

const API_BASE = 'https://localhost:7162'; // should match the// backend's dev URL

interface ShortLink {
    code: string;
    shortUrl: string;
    originalUrl: string;
    clickCount: number;
    isExpired?: boolean;
}

interface ShortenResponse {
    code: string;
    shortUrl: string;
    originalUrl: string;
    expiresAt: string | null;
}

interface UrlInfoResponse {
    code: string;
    originalUrl: string;
    createdAt: string;
    expiresAt: string | null;
    clickCount: number;
    isExpired: boolean;
}

interface ApiError {
    error: string;
}

function App() {
    const [url, setUrl] = useState('');
    const [expiresInDays, setExpiresInDays] = useState('');
    const [links, setLinks] = useState([]);
    const [error, setError] = useState('');

    useEffect(() => {
        const stored = JSON.parse(localStorage.getItem('myLinks') || '[]');
        setLinks(stored);
        refreshClickCounts(stored);
    }, []);

    async function refreshClickCounts(linkList) {
        const results  = await Promise.all(
            linkList.map(async (link) => {
                try {
                    const res = await fetch(`${API_BASE}/api/urls/${link.code}`);
                    if (res.status === 404) { // not found
                        return null;
                    }
                    
                    if (!res.ok) return link;
                    const data = await res.json();
                    return { ...link, clickCount: data.clickCount, isExpired: data.isExpired };
                } catch {
                    return link;
                }
            })
        );
        const updated = results.filter((link): link is ShortLink => link !== null);
        setLinks(updated);
        localStorage.setItem('myLinks', JSON.stringify(updated));
    }

    async function handleSubmit(e) {
        e.preventDefault();
        setError('');

        try {
            const body = { url, ...(expiresInDays ? { expiresInDays: Number(expiresInDays) } : {}) };
            const res = await fetch(`${API_BASE}/shorten`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            if (!res.ok) {
                const errBody = await res.json();
                setError(errBody.error || 'Something went wrong.');
                return;
            }

            const data = await res.json();
            const newLink = { code: data.code, shortUrl: data.shortUrl, originalUrl: data.originalUrl, clickCount: 0 };
            const updated = [newLink, ...links];
            setLinks(updated);
            localStorage.setItem('myLinks', JSON.stringify(updated));
            setUrl('');
            setExpiresInDays('');
        } catch {
            setError('Could not reach the server.');
        }
    }

    function copyToClipboard(text) {
        navigator.clipboard.writeText(text);
    }

    return (
        <div className="container">
            <div className="hero">
                <h1>Gate your links</h1>
                <p>Paste a long URL. Get a short one that routes there.</p>
            </div>

            <form onSubmit={handleSubmit} className="shorten-form">
                <input
                    type="url"
                    placeholder="https://example.com/very/long/url"
                    value={url}
                    onChange={(e) => setUrl(e.target.value)}
                    required
                />
                <input
                    type="number"
                    placeholder="Expires in days (optional)"
                    value={expiresInDays}
                    onChange={(e) => setExpiresInDays(e.target.value)}
                    min="1"
                />
                <button type="submit">Shorten</button>
            </form>

            {error && <p className="error">{error}</p>}

            <div className="board">
                <div className="board-header">
                    <h2>DEPARTURES</h2>
                    <button onClick={() => refreshClickCounts(links)}>Refresh clicks</button>
                </div>

                {links.length === 0 && <p className="board-empty">No links yet. Shorten one above.</p>}

                {links.map((link) => (
                    <div key={link.code} className={`board-row ${link.isExpired ? 'expired' : ''}`}>
                        <span className="row-code">{link.code}</span>
                        <div className="row-dest">
                            <a href={link.shortUrl} target="_blank" rel="noreferrer">{link.shortUrl}</a>
                            <span>{link.originalUrl}</span>
                        </div>
                        <div className="row-status">
                        <span className={`row-clicks ${link.isExpired ? 'is-expired' : ''}`}>
                            {link.isExpired ? 'CLOSED' : `${link.clickCount ?? 0} boarded`}
                        </span>
                            <button className="row-copy" onClick={() => copyToClipboard(link.shortUrl)}>Copy</button>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

export default App;