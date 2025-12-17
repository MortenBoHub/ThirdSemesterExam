import { useEffect, useState } from "react";
import type React from "react";

import { Card, CardHeader, CardTitle, CardContent } from "./UI/Card";
import { Button } from "./UI/Button";
import { Input } from "./UI/Input";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "./UI/Tabs";
import { Badge } from "./UI/Badge";

import { Users, Trophy, History } from "lucide-react";
import { playersApi } from "@utilities/playersApi.ts";
import { boardsApi } from "@utilities/boardsApi.ts";
import toast from "react-hot-toast";

export default function AdminPage() {
    const [drawnNumbers, setDrawnNumbers] = useState<number[]>([]);
    const [participants, setParticipants] = useState<{name: string; email: string; numbers: number[]; matches: number;}[]>([]);
    const [gameHistory, setGameHistory] = useState<{id: string; week: string; date: string; numbers: number[]; participants: number; winners: number;}[]>([]);
    const [currentWeekLabel, setCurrentWeekLabel] = useState<string>("-");
    const [onlineCount, setOnlineCount] = useState<number>(0);
    const [loading, setLoading] = useState<boolean>(false);
    const [manualInput, setManualInput] = useState("");

    // Create Player form state
    const [playerName, setPlayerName] = useState("");
    const [playerEmail, setPlayerEmail] = useState("");
    const [playerPhone, setPlayerPhone] = useState("");
    const [playerPassword, setPlayerPassword] = useState("");

    const handleManualDraw = () => {
        const num = parseInt(manualInput);

        if (
            num >= 1 &&
            num <= 16 &&
            !drawnNumbers.includes(num) &&
            drawnNumbers.length < 3
        ) {
            setDrawnNumbers([...drawnNumbers, num]);
            setManualInput("");
        }
    };

    const handleKeyPress = (e: React.KeyboardEvent) => {
        if (e.key === "Enter") handleManualDraw();
    };

    const clearDrawnNumbers = () => setDrawnNumbers([]);

    useEffect(() => {
        const load = async () => {
            setLoading(true);
            try {
                const [active, parts, history] = await Promise.all([
                    boardsApi.getActive().catch(() => null),
                    boardsApi.getParticipants().catch(() => []),
                    boardsApi.getHistory(10).catch(() => []),
                ]);

                if (active) {
                    setDrawnNumbers(active.numbers ?? []);
                    if (active.week && active.year) {
                        setCurrentWeekLabel(`Uge ${active.week} ${active.year}`);
                    } else {
                        setCurrentWeekLabel("Aktivt spil");
                    }
                } else {
                    setCurrentWeekLabel("Ingen aktiv");
                }

                const mappedParts = (parts as any[]).map(p => ({
                    name: p.name,
                    email: p.email,
                    numbers: p.numbers ?? [],
                    matches: p.matches ?? 0,
                }));
                setParticipants(mappedParts);
                setOnlineCount(mappedParts.length);

                const mappedHist = (history as any[]).map((h:any) => ({
                    id: h.boardId,
                    week: `Uge ${h.week} ${h.year}`,
                    date: new Date(h.endDate ?? h.startDate).toLocaleDateString('da-DK'),
                    numbers: h.numbers ?? [],
                    participants: h.participants ?? 0,
                    winners: h.winners ?? 0,
                }));
                setGameHistory(mappedHist);
            } catch (e:any) {
                toast.error(e?.message ?? 'Kunne ikke hente data');
            } finally {
                setLoading(false);
            }
        };
        load();
    }, []);

    return (
        <div className="space-y-6 p-4">
            {/* TOP STATS */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Card className="border-2 border-[#ed1c24]/20">
                    <CardContent className="pt-6">
                        <div className="flex items-center justify-between">
                            <div>
                                <p className="text-sm text-gray-600">Deltagere (Online)</p>
                                <p className="text-[#ed1c24]">{onlineCount} spillere</p>
                            </div>
                            <Users size={32} className="text-[#ed1c24]" />
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardContent className="pt-6">
                        <div className="flex items-center justify-between">
                            <div>
                                <p className="text-sm text-gray-600">Nuværende spil</p>
                                <p>{currentWeekLabel}</p>
                            </div>
                            <Trophy size={32} className="text-amber-500" />
                        </div>
                    </CardContent>
                </Card>
            </div>

            {/* Create Player */}
            <Card className="border">
                <CardHeader>
                    <CardTitle>Opret ny spiller</CardTitle>
                </CardHeader>
                <CardContent className="grid grid-cols-1 md:grid-cols-5 gap-2">
                    <Input placeholder="Navn" value={playerName} onChange={e=>setPlayerName(e.target.value)} />
                    <Input placeholder="Email" type="email" value={playerEmail} onChange={e=>setPlayerEmail(e.target.value)} />
                    <Input placeholder="Telefon" value={playerPhone} onChange={e=>setPlayerPhone(e.target.value)} />
                    <Input placeholder="Adgangskode" type="password" value={playerPassword} onChange={e=>setPlayerPassword(e.target.value)} />
                    <Button
                        onClick={async ()=>{
                            try{
                                const res = await playersApi.createPlayer({
                                    name: playerName,
                                    email: playerEmail,
                                    phoneNumber: playerPhone,
                                    password: playerPassword
                                });
                                toast.success(`Spiller oprettet: ${res.name}`);
                                // clear
                                setPlayerName("");
                                setPlayerEmail("");
                                setPlayerPhone("");
                                setPlayerPassword("");
                            }catch(e:any){
                                toast.error(e?.message ?? 'Kunne ikke oprette spiller');
                            }
                        }}
                        disabled={!playerName || !playerEmail || !playerPhone || playerPassword.length < 8}
                        className="w-full bg-[#ed1c24] hover:bg-[#d11920] text-white py-3 rounded-md text-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        Opret spiller
                    </Button>
                    
                </CardContent>
            </Card>

            {/* TABS */}
            <Tabs defaultValue="draw" className="w-full">
                <TabsList className="grid grid-cols-3 w-full">
                    <TabsTrigger value="draw">Træk numre</TabsTrigger>
                    <TabsTrigger value="participants">Deltagere</TabsTrigger>
                    <TabsTrigger value="history">Historik</TabsTrigger>
                </TabsList>

                {/* DRAW NUMBERS */}
                <TabsContent value="draw" className="space-y-6">
                    <Card className="border-2 border-[#ed1c24]">
                        <CardHeader className="bg-[#ed1c24]/10">
                            <CardTitle className="text-[#ed1c24]">Træk vindernumre manuelt</CardTitle>
                        </CardHeader>

                        <CardContent className="pt-10 pb-10 space-y-8">
                        {/* Number Boxes */}
                            <div className="flex justify-center space-x-40 py-48">
                                {[0, 1, 2].map((i) => (
                                    <div
                                        key={i}
                                        className={`w-60 h-60 rounded-lg flex items-center justify-center text-xl ${
                                            drawnNumbers[i]
                                                ? "bg-[#ed1c24] text-white"
                                                : "bg-gray-200 text-gray-400"
                                        }`}
                                    >
                                        {drawnNumbers[i] || "?"}
                                    </div>
                                ))}
                            </div>

                            {/* Manual input */}
                            <div className="flex items-center justify-center space-x-15 max-w-md mx-auto">
                                <Input
                                    type="number"
                                    min={1}
                                    max={16}
                                    value={manualInput}
                                    onChange={(e) => setManualInput(e.target.value)}
                                    onKeyPress={handleKeyPress}
                                    placeholder="Indtast"
                                    disabled={drawnNumbers.length >= 3}
                                    className={"text-center pr-15 text-xl"}
                                />

                                <Button
                                    onClick={handleManualDraw}
                                    disabled={!manualInput || drawnNumbers.length >= 3}
                                    className="bg-[#ed1c24] hover:bg-[#d11920]"
                                >
                                    Tilføj
                                </Button>
                            </div>

                            {/* Reset Button */}
                            {drawnNumbers.length === 3 && (
                                <div className="text-center">
                                    <Button
                                        onClick={clearDrawnNumbers}
                                        className="border border-[#ed1c24]/40
                                        text-[#ed1c24]
                                        bg-white
                                        rounded-xl
                                        px-6 py-2
                                         hover:bg-[#ed1c24]/10
                                         transition"
                                    >
                                        Bekræft numre
                                    </Button>
                                </div>
                            )}

                            {/* Winners */}
                            {drawnNumbers.length === 3 && (
                                <div className="border-t pt-6 space-y-3">
                                    <h3 className="text-lg">Vindere (Online)</h3>

                                    {participants
                                        .filter((p) => p.matches === 3)
                                        .map((winner, idx) => (
                                            <div
                                                key={idx}
                                                className="border border-green-200 bg-green-50 p-4 rounded-lg flex justify-between"
                                            >
                                                <div>
                                                    <p>{winner.name}</p>
                                                    <p className="text-sm text-gray-600">{winner.email}</p>
                                                </div>
                                                <Badge className="bg-green-600">Vinder!</Badge>
                                            </div>
                                        ))}

                                    {participants.filter((p) => p.matches === 3).length === 0 && (
                                        <p className="text-center text-gray-500">
                                            Ingen online vindere denne gang
                                        </p>
                                    )}
                                </div>
                            )}
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* PARTICIPANTS */}
                <TabsContent value="participants">
                    <Card>
                        <CardHeader>
                            <CardTitle>Deltagerliste – {currentWeekLabel}</CardTitle>
                        </CardHeader>

                        <CardContent>
                            {loading ? (
                                <div className="space-y-3">
                                    {[...Array(3)].map((_, i) => (
                                        <div key={i} className="border rounded-lg p-4 animate-pulse">
                                            <div className="flex justify-between mb-3">
                                                <div className="h-4 bg-gray-200 rounded w-40" />
                                                <div className="h-6 bg-gray-200 rounded w-20" />
                                            </div>
                                            <div className="flex gap-1">
                                                {[...Array(8)].map((_, j) => (
                                                    <div key={j} className="w-8 h-8 bg-gray-200 rounded" />
                                                ))}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            ) : participants.length === 0 ? (
                                <p className="text-center text-gray-500">Ingen deltagere fundet.</p>
                            ) : (
                                <div className="space-y-3">
                                    {participants.map((p, idx) => (
                                        <div
                                            key={idx}
                                            className="border rounded-lg p-4 hover:border-[#ed1c24]"
                                        >
                                            <div className="flex justify-between mb-3">
                                                <div>
                                                    <p>{p.name}</p>
                                                    <p className="text-sm text-gray-600">{p.email}</p>
                                                </div>

                                                {drawnNumbers.length === 3 && p.matches > 0 && (
                                                    <Badge className={p.matches === 3 ? "bg-green-600" : "bg-blue-600"}>
                                                        {p.matches} matches
                                                    </Badge>
                                                )}
                                            </div>

                                            <div className="flex items-center space-x-2">
                                                <span className="text-sm text-gray-600">Valgte numre:</span>

                                                <div className="flex flex-wrap gap-1">
                                                    {p.numbers.map((num) => (
                                                        <div
                                                            key={num}
                                                            className={`w-8 h-8 flex items-center justify-center rounded text-xs ${
                                                                drawnNumbers.includes(num)
                                                                    ? "bg-[#ed1c24] text-white"
                                                                    : "bg-gray-200"
                                                            }`}
                                                        >
                                                            {num}
                                                        </div>
                                                    ))}
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* HISTORY */}
                <TabsContent value="history">
                    <Card>
                        <CardHeader className="flex items-center space-x-2">
                            <History size={20} className="text-[#ed1c24]" />
                            <CardTitle>Al Historik</CardTitle>
                        </CardHeader>

                        <CardContent>
                            {loading ? (
                                <div className="space-y-2">
                                    {[...Array(3)].map((_, i) => (
                                        <div key={i} className="border rounded-lg p-4 animate-pulse">
                                            <div className="h-4 bg-gray-200 rounded w-32 mb-2" />
                                            <div className="h-3 bg-gray-200 rounded w-24 mb-4" />
                                            <div className="flex gap-1">
                                                {[...Array(3)].map((_, j) => (
                                                    <div key={j} className="w-8 h-8 bg-gray-200 rounded" />
                                                ))}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            ) : gameHistory.length === 0 ? (
                                <p className="text-center text-gray-500">Ingen historik at vise.</p>
                            ) : (
                                <div className="space-y-2">
                                    {gameHistory.map((game) => (
                                        <div
                                            key={game.id}
                                            className="border rounded-lg p-4 hover:border-[#ed1c24]"
                                        >
                                            <div className="flex items-center justify-between">
                                                <div className="space-y-1">
                                                    <p>{game.week}</p>
                                                    <p className="text-sm text-gray-600">{game.date}</p>

                                                    <div className="text-sm text-gray-600 flex space-x-4">
                                                        <span>{game.participants} deltagere</span>

                                                        {game.winners > 0 && (
                                                            <span className="text-green-600">{game.winners} vindere</span>
                                                        )}
                                                    </div>
                                                </div>

                                                <div className="flex space-x-1">
                                                    {game.numbers.map((n) => (
                                                        <div
                                                            key={n}
                                                            className="w-8 h-8 flex items-center justify-center bg-[#ed1c24] text-white rounded text-xs"
                                                        >
                                                            {n}
                                                        </div>
                                                    ))}
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    );
}
