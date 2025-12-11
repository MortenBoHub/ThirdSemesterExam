import React from 'react';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {}

export const Input: React.FC<InputProps> = (props) => {
    return <input {...props} className={`px-3 py-2 border rounded ${props.className}`} />;
};
