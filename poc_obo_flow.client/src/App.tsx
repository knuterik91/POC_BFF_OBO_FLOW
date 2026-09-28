import { useEffect, useState } from 'react';
import './App.css';

interface Claim {
    type: string;
    value: string;
}

interface UserProfile {
    isAuthenticated: boolean;
    name?: string;
    claims: Claim[];
}

interface Order {
    id: string;
    description: string;
    customerReference: string;
    status: string;
    orderedOn: string;
}

interface OrdersApiResponse {
    orders: Order[];
    apiClaims: Claim[];
}

interface CsrfResponse {
    token: string;
    formFieldName: string;
}

function App() {
    const [user, setUser] = useState<UserProfile>();
    const [ordersResponse, setOrdersResponse] = useState<OrdersApiResponse>();
    const [status, setStatus] = useState('Laster BFF-sesjon ...');

    useEffect(() => {
        loadUser();
    }, []);

    return (
        <div className="shell">
            <header>
                <p className="eyebrow">ASP.NET Core 10 BFF + OBO</p>
                <h1>Microsoft Entra ID referanseprosjekt</h1>
                <p>React-klienten bruker kun BFF-endepunkter og mottar aldri access tokens.</p>
            </header>

            <section className="panel">
                <h2>Sesjon</h2>
                <p>{status}</p>
                {user?.isAuthenticated ? (
                    <>
                        <p><strong>Innlogget som:</strong> {user.name ?? 'Ukjent bruker'}</p>
                        <div className="actions">
                            <button onClick={loadOrders}>Hent orders via BFF OBO</button>
                            <button className="secondary" onClick={logout}>Logg ut</button>
                        </div>
                    </>
                ) : (
                    <a className="button" href="/bff/login">Logg inn med Microsoft Entra ID</a>
                )}
            </section>

            {user?.isAuthenticated && (
                <section className="panel">
                    <h2>Claims i BFF-cookie-sesjonen</h2>
                    <ClaimsTable claims={user.claims} />
                </section>
            )}

            {ordersResponse && (
                <>
                    <section className="panel">
                        <h2>Orders fra API</h2>
                        <table>
                            <thead>
                                <tr>
                                    <th>Id</th>
                                    <th>Beskrivelse</th>
                                    <th>Kundereferanse</th>
                                    <th>Status</th>
                                    <th>Dato</th>
                                </tr>
                            </thead>
                            <tbody>
                                {ordersResponse.orders.map(order => (
                                    <tr key={order.id}>
                                        <td>{order.id}</td>
                                        <td>{order.description}</td>
                                        <td>{order.customerReference}</td>
                                        <td>{order.status}</td>
                                        <td>{order.orderedOn}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </section>

                    <section className="panel">
                        <h2>JWT-claims mottatt i API</h2>
                        <ClaimsTable claims={ordersResponse.apiClaims} />
                    </section>
                </>
            )}
        </div>
    );

    async function loadUser() {
        try {
            const response = await fetch('/bff/user', { credentials: 'same-origin' });

            if (!response.ok) {
                setStatus('Ikke innlogget. Start Entra ID-login via BFF.');
                return;
            }

            const profile = await response.json() as UserProfile;
            setUser(profile);
            setStatus(profile.isAuthenticated ? 'Aktiv HttpOnly BFF-cookie-sesjon.' : 'Ikke innlogget.');
        } catch {
            setStatus('Ikke innlogget, eller BFF svarer ikke ennå.');
        }
    }

    async function loadOrders() {
        setStatus('Henter orders fra API via OBO ...');

        const response = await fetch('/bff/orders', { credentials: 'same-origin' });

        if (response.ok) {
            setOrdersResponse(await response.json() as OrdersApiResponse);
            setStatus('Orders hentet fra API via BFF og OBO-token.');
            return;
        }

        setStatus(`Kunne ikke hente orders. HTTP ${response.status}. Kontroller innlogging og API-scope.`);
    }

    async function logout() {
        const csrfResponse = await fetch('/bff/csrf', { credentials: 'same-origin' });

        if (!csrfResponse.ok) {
            setStatus('Kunne ikke hente CSRF-token for logout.');
            return;
        }

        const csrf = await csrfResponse.json() as CsrfResponse;
        const form = document.createElement('form');
        form.method = 'post';
        form.action = '/bff/logout';

        const token = document.createElement('input');
        token.type = 'hidden';
        token.name = csrf.formFieldName;
        token.value = csrf.token;
        form.appendChild(token);

        document.body.appendChild(form);
        form.submit();
    }
}

function ClaimsTable({ claims }: { claims: Claim[] }) {
    return (
        <table>
            <thead>
                <tr>
                    <th>Type</th>
                    <th>Verdi</th>
                </tr>
            </thead>
            <tbody>
                {claims.map((claim, index) => (
                    <tr key={`${claim.type}-${index}`}>
                        <td>{claim.type}</td>
                        <td className="claim-value">{claim.value}</td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
}

export default App;