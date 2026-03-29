import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('hata_orani');
const eventsDuration = new Trend('events_sure_ms');
const ticketsDuration = new Trend('tickets_sure_ms');

const VUS = parseInt(__ENV.VUS || '50');

export const options = {
  stages: [
    { duration: '15s', target: VUS },   // Ramp up
    { duration: '45s', target: VUS },   // Hold
    { duration: '10s', target: 0   },   // Ramp down
  ],
  thresholds: {
    http_req_duration: [`p(95)<2000`],
    http_req_failed:   ['rate<0.20'],
    hata_orani:        ['rate<0.20'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const API_KEY  = __ENV.API_KEY  || 'KingoSifre123';

const headers = {
  'X-Api-Key':    API_KEY,
  'Content-Type': 'application/json',
};

export default function () {
  // 1) Login
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ Username: 'admin', Password: 'Bilet2026' }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  const loginOk = check(loginRes, {
    'Login 200': (r) => r.status === 200,
    'Token var': (r) => r.json('token') !== undefined,
  });
  errorRate.add(!loginOk);

  let jwtHeaders = { ...headers };
  if (loginOk) {
    jwtHeaders['Authorization'] = `Bearer ${loginRes.json('token')}`;
  }

  sleep(0.2);

  // 2) Etkinlikleri listele
  const eventsRes = http.get(`${BASE_URL}/api/events`, { headers });
  const eventsOk = check(eventsRes, {
    'GET /api/events 200': (r) => r.status === 200,
  });
  errorRate.add(!eventsOk);
  eventsDuration.add(eventsRes.timings.duration);

  sleep(0.2);

  // 3) Biletleri listele
  const ticketsRes = http.get(`${BASE_URL}/api/tickets`, { headers: jwtHeaders });
  const ticketsOk = check(ticketsRes, {
    'GET /api/tickets 200': (r) => r.status === 200,
  });
  errorRate.add(!ticketsOk);
  ticketsDuration.add(ticketsRes.timings.duration);

  sleep(0.2);

  // 4) Yeni bilet oluştur
  const postRes = http.post(
    `${BASE_URL}/api/tickets`,
    JSON.stringify({
      eventName:    'Stres Testi Konseri',
      customerName: `stres-kullanici-${__VU}`,
      seat:         `ST-${__VU}-${__ITER}`,
      status:       'Active',
      price:        250,
    }),
    { headers: jwtHeaders }
  );
  check(postRes, {
    'POST /api/tickets 201': (r) => r.status === 201,
  });

  sleep(0.5);
}

export function handleSummary(data) {
  const filename = `k6/results/rapor-${VUS}vus.json`;
  return {
    [filename]: JSON.stringify(data, null, 2),
    stdout: textSummary(data, VUS),
  };
}

function textSummary(data, vus) {
  const reqs    = data.metrics.http_reqs?.values?.count ?? 0;
  const errRate = ((data.metrics.http_req_failed?.values?.rate ?? 0) * 100).toFixed(2);
  const avg     = (data.metrics.http_req_duration?.values?.avg ?? 0).toFixed(2);
  const p95     = (data.metrics.http_req_duration?.values?.['p(95)'] ?? 0).toFixed(2);
  const maxDur  = (data.metrics.http_req_duration?.values?.max ?? 0).toFixed(2);
  const rps     = (data.metrics.http_reqs?.values?.rate ?? 0).toFixed(2);

  return `
╔══════════════════════════════════════════════════╗
║   BİLET SİSTEMİ STRES TESTİ — ${String(vus).padEnd(4)} VU          ║
╠══════════════════════════════════════════════════╣
║  Toplam İstek  : ${String(reqs).padEnd(30)} ║
║  İstek/sn      : ${String(rps).padEnd(30)} ║
║  Hata Oranı    : ${String(errRate + '%').padEnd(30)} ║
║  Ort. Süre     : ${String(avg + 'ms').padEnd(30)} ║
║  P95 Süre      : ${String(p95 + 'ms').padEnd(30)} ║
║  Max Süre      : ${String(maxDur + 'ms').padEnd(30)} ║
╚══════════════════════════════════════════════════╝
`;
}
