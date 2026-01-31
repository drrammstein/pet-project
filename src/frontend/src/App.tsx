import React, { useState, ChangeEvent, FormEvent } from 'react';
import './App.css';

// Интерфейс для данных формы
interface LoginFormData {
    username: string;
    password: string;
}

function LoginForm() {
    const [formData, setFormData] = useState<LoginFormData>({
        username: '',
        password: ''
    });

    // Явно указываем тип события
    const handleInputChange = (e: ChangeEvent<HTMLInputElement>) => {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value
        });
    };

    const handleLogin = async (e: FormEvent) => {
        e.preventDefault();
        console.log('Login attempt:', formData);
    };

    const handleRegister = async (e: FormEvent) => {
        e.preventDefault();
        console.log('Register attempt:', formData);
    };

    const handleForgotPassword = (e: FormEvent) => {
        e.preventDefault();
        console.log('Forgot password for:', formData.username);
    };

    return (
        <div className="login-container">
            <form className="login-form">
                <h2>Авторизация</h2>

                <div className="form-group">
                    <label htmlFor="username">Логин:</label>
                    <input
                        type="text"
                        id="username"
                        name="username"
                        value={formData.username}
                        onChange={handleInputChange}
                        placeholder="Введите логин"
                    />
                </div>

                <div className="form-group">
                    <label htmlFor="password">Пароль:</label>
                    <input
                        type="password"
                        id="password"
                        name="password"
                        value={formData.password}
                        onChange={handleInputChange}
                        placeholder="Введите пароль"
                    />
                </div>

                <div className="buttons-group">
                    <button className="btn btn-primary" onClick={handleLogin}>
                        Войти
                    </button>
                    <button className="btn btn-secondary" onClick={handleRegister}>
                        Зарегистрироваться
                    </button>
                    <button className="btn btn-link" onClick={handleForgotPassword}>
                        Восстановить пароль
                    </button>
                </div>
            </form>
        </div>
    );
}

function App() {
    return <LoginForm />;
}

export default App;