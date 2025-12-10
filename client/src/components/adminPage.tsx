import { useState } from "react";
import type React from "react";

import { Card, CardHeader, CardTitle, CardContent } from "./UI/Card";
import { Button } from "./UI/Button";
import { Input } from "./UI/Input";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "./UI/Tabs";
import { Badge } from "./UI/Badge";

import { Users, Trophy, History } from "lucide-react";

export default function AdminPage() {
    const [drawnNumbers, setDrawnNumbers] = useState<number[]>([]);
    const [manualInput, setManualInput] = useState("");

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

    // MOCK DATA (replace later with backend)
    const participants = [
        { name: "Anders Nielsen", numbers: [1, 4, 7, 10, 13], matches: 2, email: "anders@email.dk" },
        { name: "Maria Jensen", numbers: [2, 5, 8, 11, 14, 16], matches: 1, email: "maria@email.dk" },
        { name: "Peter Hansen", numbers: [3, 6, 9, 12, 15], matches: 0, email: "peter@email.dk" },
        { name: "Laura Andersen", numbers: [1, 2, 3, 4, 5], matches: 3, email: "laura@email.dk" },
        { name: "Thomas Larsen", numbers: [7, 8, 9, 10, 11, 12], matches: 1, email: "thomas@email.dk" },
    ];

    const gameHistory = [
        { id: 1, week: "Uge 46 2024", date: "18.11.2024", numbers: [3, 7, 12], participants: 5, winners: 0 },
        { id: 2, week: "Uge 45 2024", date: "11.11.2024", numbers: [1, 8, 15], participants: 4, winners: 0 },
        { id: 3, week: "Uge 44 2024", date: "04.11.2024", numbers: [4, 9, 13], participants: 6, winners: 1 },
        { id: 4, week: "Uge 43 2024", date: "28.10.2024", numbers: [2, 6, 14], participants: 5, winners: 0 },
    ];

    return (
        <div className="space-y-6 p-4">
            {/* TOP STATS */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Card className="border-2 border-[#ed1c24]/20">
                    <CardContent className="pt-6">
                        <div className="flex items-center justify-between">
                            <div>
                                <p className="text-sm text-gray-600">Deltagere (Online)</p>
                                <p className="text-[#ed1c24]">5 spillere</p>
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
                                <p>Uge 46 2024</p>
                            </div>
                            <Trophy size={32} className="text-amber-500" />
                        </div>
                    </CardContent>
                </Card>
            </div>

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

                        <CardContent className="space-y-6 pt-6">
                            {/* Number Boxes */}
                            <div className="flex justify-center space-x-4 py-8">
                                {[0, 1, 2].map((i) => (
                                    <div
                                        key={i}
                                        className={`w-20 h-20 rounded-lg flex items-center justify-center text-xl ${
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
                            <div className="flex space-x-2 max-w-md mx-auto">
                                <Input
                                    type="number"
                                    min={1}
                                    max={16}
                                    value={manualInput}
                                    onChange={(e) => setManualInput(e.target.value)}
                                    onKeyPress={handleKeyPress}
                                    placeholder="Indtast nummer (1–16)"
                                    disabled={drawnNumbers.length >= 3}
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
                                        className="border-[#ed1c24] text-[#ed1c24]"
                                    >
                                        Nulstil til nyt spil
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
                            <CardTitle>Deltagerliste – Uge 46 2024</CardTitle>
                        </CardHeader>

                        <CardContent>
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
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    );
}
