import React from 'react';

export const Card: React.FC<{ children: React.ReactNode; className?: string }> = ({ children, className }) => {
    return <div className={`bg-white shadow rounded ${className}`}>{children}</div>;
};

export const CardHeader: React.FC<{ children: React.ReactNode; className?: string }> = ({ children, className }) => {
    return <div className={`p-4 border-b ${className}`}>{children}</div>;
};

export const CardContent: React.FC<{ children: React.ReactNode; className?: string }> = ({ children, className }) => {
    return <div className={`p-4 ${className}`}>{children}</div>;
};

export const CardTitle: React.FC<{ children: React.ReactNode; className?: string }> = ({ children, className }) => {
    return <h2 className={`text-lg font-bold ${className}`}>{children}</h2>;
};
