import type React from 'react';

interface NumberBoardProps {
    selectedNumbers: number[];
    onNumberSelect: (num: number) => void;
    drawnNumbers?: number[];
    readonly?: boolean;
}

export function NumberBoard({
                                selectedNumbers,
                                onNumberSelect,
                                drawnNumbers = [],
                                readonly = false,
                            }: NumberBoardProps) {
    return (
        <div className="grid grid-cols-4 gap-3">
            {Array.from({ length: 16 }, (_, i) => i + 1).map((num) => {
                const isSelected = selectedNumbers.includes(num);
                const isDrawn = drawnNumbers.includes(num);

                return (
                    <button
                        key={num}
                        onClick={() => !readonly && onNumberSelect(num)}
                        disabled={readonly}
                        className={`
              aspect-square rounded-lg transition-all duration-200 
              flex items-center justify-center
              ${
                            isDrawn
                                ? 'bg-[#ed1c24] text-white ring-4 ring-[#ed1c24]/30 scale-105'
                                : isSelected
                                    ? 'bg-[#d9894a] text-white hover:bg-[#c97939] shadow-md'
                                    : 'bg-[#e8ddc4] text-gray-700 hover:bg-[#dccfb0] hover:scale-105'
                        }
              ${!readonly && 'cursor-pointer active:scale-95'}
              ${readonly && 'cursor-default'}
            `}
                    >
                        <span>{num}</span>
                    </button>
                );
            })}
        </div>
    );
}