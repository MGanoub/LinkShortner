import { useState, useEffect } from 'react';
import './App.css';

const API_BASE = import.meta.env.VITE_API_BASE;

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

interface AuthResponse {
    token: string;
    email: string;
}

interface ApiError {
    error: string;
}

function App() {
    const [url, setUrl] = useState('');
    const [expiresInDays, setExpiresInDays] = useState('');
    const [links, setLinks] = useState<ShortLink[]>([]);
    const [error, setError] = useState('');

    const [token, setToken] = useState<string | null>(() => localStorage.getItem('authToken'));
    const [email, setEmail] = useState<string | null>(() => localStorage.getItem('authEmail'));

    const [showAuth, setShowAuth] = useState(false);
    const [authMode, setAuthMode] = useState<'login' | 'register'>('login');
    const [authEmail, setAuthEmail] = useState('');
    const [authPassword, setAuthPassword] = useState('');
    const [authError, setAuthError] = useState('');
    const [pendingSubmit, setPendingSubmit] = useState(false);

    useEffect(() => {
        if (token) {
            refreshMyLinks(token);
        }
    }, [token]);

    async function refreshMyLinks(currentToken: string) {
        try {
            const res = await fetch(`${API_BASE}/api/urls/mine`, {
                headers: { Authorization: `Bearer ${currentToken}` },
            });

            if (res.status === 401) {
                logOut();
                return;
            }

            if (!res.ok) return;

            const data: ShortLink[] = await res.json();
            setLinks(data);
        } catch {
            // network error — leave existing list as-is
        }
    }

    function logOut() {
        setToken(null);
        setEmail(null);
        setLinks([]);
        localStorage.removeItem('authToken');
        localStorage.removeItem('authEmail');
    }

    async function handleAuthSubmit(e: React.FormEvent) {
        e.preventDefault();
        setAuthError('');

        try {
            const endpoint = authMode === 'login' ? '/auth/login' : '/auth/register';
            const res = await fetch(`${API_BASE}${endpoint}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: authEmail, password: authPassword }),
            });

            if (!res.ok) {
                if (res.status === 401) {
                    setAuthError('Incorrect email or password.');
                } else {
                    const errBody: ApiError = await res.json();
                    setAuthError(errBody.error || 'Something went wrong.');
                }
                return;
            }

            const data: AuthResponse = await res.json();
            setToken(data.token);
            setEmail(data.email);
            localStorage.setItem('authToken', data.token);
            localStorage.setItem('authEmail', data.email);

            setShowAuth(false);
            setAuthEmail('');
            setAuthPassword('');

            if (pendingSubmit) {
                setPendingSubmit(false);
                await submitShorten(data.token);
            }
        } catch {
            setAuthError('Could not reach the server.');
        }
    }

    async function submitShorten(activeToken: string) {
        setError('');

        try {
            const body = { url, ...(expiresInDays ? { expiresInDays: Number(expiresInDays) } : {}) };
            const res = await fetch(`${API_BASE}/shorten`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${activeToken}`,
                },
                body: JSON.stringify(body),
            });

            if (res.status === 401) {
                logOut();
                setError('Your session expired. Please log in again.');
                return;
            }

            if (!res.ok) {
                const errBody: ApiError = await res.json();
                setError(errBody.error || 'Something went wrong.');
                return;
            }
            await res.json();
            setUrl('');
            setExpiresInDays('');
            await refreshMyLinks(activeToken);
        } catch {
            setError('Could not reach the server.');
        }
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();

        if (!token) {
            setPendingSubmit(true);
            setShowAuth(true);
            return;
        }

        await submitShorten(token);
    }

    function copyToClipboard(text: string) {
        navigator.clipboard.writeText(text);
    }

    return (
        <div className="container">
            <div className="hero">
                <div className="hero-top">
                    <h1>Gate your links</h1>
                    {token ? (
                        <div className="account">
                            <span>{email}</span>
                            <button onClick={logOut}>Log out</button>
                        </div>
                    ) : (
                        <button className="login-link" onClick={() => { setAuthMode('login'); setShowAuth(true); }}>
                            Log in
                        </button>
                    )}
                </div>
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

            {showAuth && (
                <div className="auth-overlay" onClick={() => setShowAuth(false)}>
                    <div className="auth-panel" onClick={(e) => e.stopPropagation()}>
                        <div className="auth-tabs">
                            <button
                                className={authMode === 'login' ? 'active' : ''}
                                onClick={() => setAuthMode('login')}
                                type="button"
                            >
                                Log in
                            </button>
                            <button
                                className={authMode === 'register' ? 'active' : ''}
                                onClick={() => setAuthMode('register')}
                                type="button"
                            >
                                Sign up
                            </button>
                        </div>

                        <form onSubmit={handleAuthSubmit}>
                            <input
                                type="email"
                                placeholder="Email"
                                value={authEmail}
                                onChange={(e) => setAuthEmail(e.target.value)}
                                required
                            />
                            <input
                                type="password"
                                placeholder="Password"
                                value={authPassword}
                                onChange={(e) => setAuthPassword(e.target.value)}
                                minLength={authMode === 'register' ? 8 : undefined}
                                required
                            />
                            {authError && <p className="error">{authError}</p>}
                            <button type="submit">{authMode === 'login' ? 'Log in' : 'Create account'}</button>
                        </form>

                        {pendingSubmit && (
                            <p className="auth-hint">You'll be signed in and your link will be created right after.</p>
                        )}
                    </div>
                </div>
            )}

            <div className="board">
                <div className="board-header">
                    <h2>Shorts</h2>
                    {token && <button onClick={() => refreshMyLinks(token)}>Refresh clicks</button>}
                </div>

                {!token && <p className="board-empty">Log in to see your links here.</p>}
                {token && links.length === 0 && <p className="board-empty">No links yet. Shorten one above.</p>}

                {links.map((link) => (
                    <div key={link.code} className={`board-row ${link.isExpired ? 'expired' : ''}`}>
                        <span className="row-code">{link.code}</span>
                        <div className="row-dest">
                            <a href={link.shortUrl} target="_blank" rel="noreferrer">{link.shortUrl}</a>
                            <span>{link.originalUrl}</span>
                        </div>
                        <div className="row-status">
                            <span className={`row-clicks ${link.isExpired ? 'is-expired' : ''}`}>
                                {link.isExpired ? 'CLOSED' : `${link.clickCount ?? 0} Visited`}
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