import React from 'react';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {}

export const Input: React.FC<InputProps> = (props) => {
    return <input {...props} className={`px-10 py-5 border rounded ${props.className}`} />;
};
