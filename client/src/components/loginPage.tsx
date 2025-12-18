import { useState } from 'react';
import type React from 'react';
import { Button } from './UI/Button';
import { Input } from './UI/Input';
import { Card, CardContent, CardHeader, CardTitle } from './UI/Card';
import { Lock, User } from 'lucide-react';
import { authApi } from "@utilities/authApi.ts";
import toast from "react-hot-toast";

interface LoginScreenProps {
    // made optional so you can use <LoginPage /> without passing it yet
    onLogin: (username: string, isAdmin: boolean) => void;
}

export default function LoginPage({ onLogin }: LoginScreenProps) {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        if (!username || !password) {
            setError('Indtast Email og kodeord');
            return;
        }

        try {
            setLoading(true);
            const res = await authApi.login({ email: username, password });
            if (!res?.token) {
                throw new Error('Login mislykkedes');
            }
            localStorage.setItem('jwt', res.token);
            toast.success('Logget ind');
            // Notify parent so it can call whoAmI() and update app state
            onLogin?.(username, false);
        } catch (err: any) {
            // Best-effort message
            const msg = err?.message || 'Ugyldig Email eller kodeord';
            setError(msg);
            toast.error(msg);
        } finally {
            setLoading(false);
        }
    };

    const handleKeyPress = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') {
            handleSubmit(e as any);
        }
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-[#f5f1e8] via-[#f5f1e8] to-[#ed1c24]/10 flex items-center justify-center p-4">
            <Card className="w-full max-w-md border-2 border-[#ed1c24]/20 shadow-2xl">
                <CardHeader className="space-y-6 pb-8">
                    {/* Logo placeholder */}
                    <div className="flex justify-center">
                        <div className="w-24 h-24 bg-gradient-to-br from-[#ed1c24] to-[#d11920] rounded-full flex items-center justify-center shadow-lg">
                            <span className="text-white text-5xl">🎯</span>
                        </div>
                    </div>

                    <CardTitle className="text-center text-[#ed1c24]">
                        Døde Duer
                    </CardTitle>
                    <p className="text-center text-gray-600 text-sm">
                        Log ind for at fortsætte
                    </p>
                </CardHeader>

                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-6 flex flex-col items-center">
                        <div className="space-y-4">
                            {/* Username field */}
                            <div className="space-y-2">
                                <label className="text-sm flex items-center space-x-2 text-gray-700">
                                    <User size={16} className="text-[#ed1c24]" />
                                    <span>Email</span>
                                </label>
                                <Input
                                    type="text"
                                    value={username}
                                    onChange={(e) => setUsername(e.target.value)}
                                    onKeyPress={handleKeyPress}
                                    placeholder="Indtast Email"
                                    className="h-12 text-center border-2 focus:border-[#ed1c24]"
                                    autoFocus
                                />
                            </div>

                            {/* Password field */}
                            <div className="space-y-2">
                                <label className="text-sm flex items-center space-x-2 text-gray-700">
                                    <Lock size={16} className="text-[#ed1c24]" />
                                    <span>Kodeord</span>
                                </label>
                                <Input
                                    type="password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    onKeyPress={handleKeyPress}
                                    placeholder="Indtast kodeord"
                                    className="h-12 text-center border-2 focus:border-[#ed1c24]"
                                />
                            </div>
                        </div>

                        {/* Error message */}
                        {error && (
                            <div className="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-600 text-center">
                                {error}
                            </div>
                        )}

                        {/* Login button */}
                        <Button
                            type="submit"
                            disabled={loading}
                            className="w-full h-12 flex items-center justify-center bg-[#ed1c24] hover:bg-[#d11920] transition-all shadow-md hover:shadow-lg text-lg disabled:opacity-60 disabled:cursor-not-allowed"

                        >
                            {loading ? 'Logger ind…' : 'Log ind'}
                        </Button>

                        
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
