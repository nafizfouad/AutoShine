import Sidebar from './Sidebar';

export default function AppLayout({ children, title }) {
  return (
    <div className="app-layout">
      <Sidebar />
      <div className="main-content">
        <header className="topbar">
          <h1 className="topbar-title">{title}</h1>
        </header>
        <main className="page fade-in">{children}</main>
      </div>
    </div>
  );
}
