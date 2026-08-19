import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

const GITHUB_URL = 'https://github.com/fernandesnic/ProjectBee'
const GITHUB_PROFILE_URL = 'https://github.com/fernandesnic'
const LINKEDIN_URL = 'https://linkedin.com/in/fernandesnic'

const SWAGGER_URL: string | undefined =
  import.meta.env.VITE_SWAGGER_URL ??
  (import.meta.env.DEV ? 'http://localhost:5054/swagger' : undefined)

const NAV_LINKS = [
  { href: '#modulos', label: 'Módulos' },
  { href: '#como-funciona', label: 'Como funciona' },
  { href: '#tecnico', label: 'Detalhes técnicos' },
]

const STACK_DECISIONS = [
  {
    tech: '.NET 10 + ASP.NET Core Minimal APIs',
    reason:
      'Poucas rotas por módulo ainda não justificam o peso de controllers — dá pra migrar pra eles quando a complexidade pedir.',
  },
  {
    tech: 'Entity Framework Core',
    reason:
      'Migrations versionadas em vez de script de banco solto, e LINQ tipado em vez de SQL escrito à mão em cada endpoint.',
  },
  {
    tech: 'SQL Server',
    reason:
      'Banco relacional com integridade forte — chave composta e precisão decimal garantidas pelo schema, não simuladas em código.',
  },
  {
    tech: 'ASP.NET Core Identity + JWT',
    reason:
      'Identity cuida de hash de senha e cadastro de usuário; JWT evita guardar sessão no servidor entre requisições.',
  },
  {
    tech: 'FluentValidation',
    reason:
      'Regra de validação separada do DTO e do endpoint — mais fácil de testar e de reaproveitar entre criação e atualização.',
  },
  {
    tech: 'React',
    reason:
      'Frontend consumindo a API como qualquer outro cliente, sem acoplar interface ao backend.',
  },
]

const DEMO_ROWS = [
  { produto: 'Mel Silvestre 500g', sku: 'MEL-500-SIL', armazem: 'SP1', lote: 'LOTE-2026-001', saldo: 120 },
  { produto: 'Mel Silvestre 500g', sku: 'MEL-500-SIL', armazem: 'SRQ', lote: 'LOTE-2026-002', saldo: 45 },
  { produto: 'Própolis Extrato 30ml', sku: 'PROP-30-EXT', armazem: 'SP1', lote: 'LOTE-2026-004', saldo: 310 },
  { produto: 'Cera de Abelha 1kg', sku: 'CERA-1000', armazem: 'SRQ', lote: 'LOTE-2026-003', saldo: 18 },
]

const PAIN_POINTS = [
  'Você compra o que já tinha parado em outro armazém.',
  'Vende o que não tem e descobre na hora de separar.',
  'O lote vence no fundo do galpão porque ninguém sabia que estava lá.',
]

const STEPS = [
  {
    title: 'Cadastre produtos e armazéns',
    description:
      'Defina onde cada produto pode existir e comece a rastrear por lote desde a primeira entrada.',
  },
  {
    title: 'Registre entradas e saídas',
    description:
      'Toda movimentação atualiza o saldo na hora, direto na base que o resto do sistema vai consumir.',
  },
  {
    title: 'Acompanhe o saldo consolidado',
    description:
      'Veja quanto tem, onde tem e em qual lote, por armazém ou no total da operação.',
  },
]

const MODULES = [
  {
    status: 'Disponível',
    title: 'Estoque multi-armazém',
    description:
      'Saldo por produto, armazém e lote, sempre atualizado. É a base sobre a qual os próximos módulos vão rodar.',
    active: true,
  },
  {
    status: 'Planejado',
    title: 'Faturamento',
    description:
      'Emissão de nota consumindo o saldo do estoque direto, sem passar dado de um sistema pro outro na mão.',
    active: false,
  },
  {
    status: 'Planejado',
    title: 'Financeiro',
    description:
      'Contas a pagar, a receber e fluxo de caixa alimentados pelo que acontece no faturamento e no estoque.',
    active: false,
  },
]

const DONE = [
  'Saldo de estoque com chave composta (produto + armazém + lote)',
  'CRUD de produtos e armazéns',
  'Login e registro com Identity, token JWT com claims de role',
  'Validação assíncrona de SKU duplicado antes de gravar',
  'Erros da API padronizados em ProblemDetails (RFC 7807)',
]

const NEXT = [
  'Autorização por perfil nas rotas de escrita (hoje é só "autenticado")',
  'Módulo de faturamento consumindo o saldo do estoque',
  'Dashboard financeiro: contas a pagar/receber, fluxo de caixa',
]

const DECISIONS = [
  {
    title: 'Saldo é uma entidade própria, não um campo do produto',
    description:
      'A chave é composta por produto, armazém e lote. Dava pra modelar como um número solto no produto, mas aí o mesmo item não poderia existir em quantidades diferentes, em lugares diferentes, sem gambiarra.',
  },
  {
    title: 'Erro de API sempre no mesmo formato (RFC 7807)',
    description:
      'Falha de validação e exceção não tratada respondem com a mesma estrutura de erro. Quem consome a API não precisa tratar cada tipo de falha de um jeito diferente.',
  },
  {
    title: 'SKU é validado antes de gravar, não só na constraint do banco',
    description:
      'Uma unique constraint garante integridade, mas devolve erro genérico. Consultar o banco antes permite responder com uma mensagem que faz sentido pra quem está cadastrando.',
  },
  {
    title: 'Perfis de acesso já no token, antes de ter rota que os use',
    description:
      'O JWT carrega a role do usuário desde o primeiro módulo, mesmo sem nenhuma rota diferenciando operador de gestor ainda. A ideia é não ter que voltar na autenticação quando isso virar necessário.',
  },
]

function Hex({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 100 100" className={className} aria-hidden="true">
      <polygon points="50,3 93,26 93,74 50,97 7,74 7,26" />
    </svg>
  )
}

function Logo() {
  return (
    <span className="inline-flex items-center gap-2 font-semibold text-lg tracking-tight">
      <svg viewBox="0 0 48 48" className="h-7 w-7">
        <polygon points="24,3 43,14 43,34 24,45 5,34 5,14" className="fill-hive" />
        <polygon
          points="24,10 37,17.5 37,32.5 24,40 11,32.5 11,17.5"
          className="fill-none stroke-honey"
          strokeWidth="2.5"
        />
        <circle cx="24" cy="25" r="4.5" className="fill-honey" />
      </svg>
      ProjectBee
    </span>
  )
}

type SectionProps = {
  id?: string
  eyebrow: string
  title: string
  tone?: 'light' | 'dark'
  spacing?: 'tight' | 'normal' | 'loose'
  className?: string
  children: ReactNode
}

const SECTION_PADDING = {
  tight: 'py-16',
  normal: 'py-20',
  loose: 'py-24',
}

function Section({
  id,
  eyebrow,
  title,
  tone = 'light',
  spacing = 'normal',
  className = '',
  children,
}: SectionProps) {
  const eyebrowColor = tone === 'dark' ? 'text-honey-light' : 'text-honey-dark'
  const titleColor = tone === 'dark' ? 'text-cream' : 'text-ink'

  const borderColor = tone === 'dark' ? 'border-cream/10' : 'border-line'

  return (
    <section id={id} className={`scroll-mt-20 border-b ${borderColor} ${className}`}>
      <div className={`mx-auto max-w-6xl px-6 ${SECTION_PADDING[spacing]}`}>
        <p className={`text-sm font-semibold uppercase tracking-widest ${eyebrowColor}`}>
          {eyebrow}
        </p>
        <h2
          className={`mt-4 max-w-2xl text-3xl font-bold leading-tight tracking-tight sm:text-4xl ${titleColor}`}
        >
          {title}
        </h2>
        {children}
      </div>
    </section>
  )
}

function DemoTable() {
  return (
    <div className="overflow-hidden rounded-2xl border border-cream/10 bg-cream text-ink shadow-2xl shadow-black/30">
      <div className="flex items-center justify-between border-b border-line px-5 py-3">
        <span className="text-xs font-semibold uppercase tracking-widest text-ink-muted">
          Saldo por armazém
        </span>
        <span className="flex gap-1">
          <span className="h-2 w-2 rounded-full bg-line" />
          <span className="h-2 w-2 rounded-full bg-line" />
          <span className="h-2 w-2 rounded-full bg-honey" />
        </span>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs">
          <thead>
            <tr className="border-b border-line text-ink-muted">
              <th className="px-5 py-2 font-medium">Produto</th>
              <th className="px-3 py-2 font-medium">Armazém</th>
              <th className="hidden px-3 py-2 font-medium sm:table-cell">Lote</th>
              <th className="px-5 py-2 text-right font-medium">Saldo</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {DEMO_ROWS.map((row) => (
              <tr key={`${row.sku}-${row.armazem}-${row.lote}`}>
                <td className="px-5 py-3">
                  <div className="font-medium text-ink">{row.produto}</div>
                  <div className="text-ink-muted">{row.sku}</div>
                </td>
                <td className="px-3 py-3 font-mono text-ink-muted">{row.armazem}</td>
                <td className="hidden px-3 py-3 font-mono text-ink-muted sm:table-cell">
                  {row.lote}
                </td>
                <td className="px-5 py-3 text-right font-semibold tabular-nums text-ink">
                  {row.saldo}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function LandingPage() {
  return (
    <div className="min-h-screen bg-cream text-ink">
      {/* Nav */}
      <header className="sticky top-0 z-20 border-b border-line/70 bg-cream/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Logo />
          <nav className="flex items-center gap-6">
            {NAV_LINKS.map(({ href, label }) => (
              <a
                key={href}
                href={href}
                className="hidden text-sm text-ink-muted transition hover:text-ink sm:inline"
              >
                {label}
              </a>
            ))}
            <Link
              to="/login"
              className="rounded-full border border-line px-4 py-1.5 text-sm font-medium text-ink transition hover:border-honey-dark hover:text-honey-dark"
            >
              Entrar
            </Link>
          </nav>
        </div>
      </header>

      {/* ================= PARTE 1 — PRODUTO ================= */}

      {/* Hero */}
      <section className="relative overflow-hidden bg-hive text-cream">
        <Hex className="pointer-events-none absolute -right-16 -top-24 h-80 w-80 fill-none stroke-honey/15 stroke-[1.5]" />
        <Hex className="pointer-events-none absolute -bottom-28 -left-20 h-72 w-72 fill-none stroke-honey/10 stroke-[1.5]" />

        <div className="relative mx-auto grid max-w-6xl gap-14 px-6 py-24 sm:py-28 lg:grid-cols-[1.1fr_1fr] lg:items-center lg:gap-12">
          <div>
            <span className="inline-flex items-center rounded-full border border-honey/30 bg-honey/10 px-3 py-1 text-xs font-medium tracking-wide text-honey-light">
              Controle de estoque multi-armazém
            </span>

            <h1 className="mt-6 text-4xl font-extrabold leading-[1.1] tracking-tight sm:text-6xl">
              Saiba exatamente quanto você tem, em cada armazém, agora.
            </h1>

            <p className="mt-6 max-w-xl text-lg text-cream/70 sm:text-xl">
              ProjectBee centraliza o saldo dos seus produtos por armazém e
              lote em um só lugar, sempre atualizado.
            </p>

            <div className="mt-10 flex flex-wrap items-center gap-x-6 gap-y-3">
              <Link
                to="/login?demo=1"
                className="inline-flex items-center rounded-full bg-honey px-7 py-3 font-semibold text-hive shadow-lg shadow-honey/20 transition hover:bg-honey-light"
              >
                Entrar na demo
              </Link>
              <a
                href="#como-funciona"
                className="text-sm font-medium text-cream/60 underline decoration-cream/30 underline-offset-4 transition hover:text-cream"
              >
                Ver como funciona ↓
              </a>
            </div>
          </div>

          <DemoTable />
        </div>
      </section>

      {/* O problema */}
      <Section
        eyebrow="O problema"
        title="Cada armazém novo é uma nova fonte de erro."
        spacing="tight"
        className="bg-cream"
      >
        <ul className="mt-8 max-w-xl space-y-4">
          {PAIN_POINTS.map((point) => (
            <li key={point} className="flex items-start gap-3 text-base leading-relaxed text-ink">
              <Hex className="mt-1 h-3.5 w-3.5 shrink-0 fill-honey-dark" />
              {point}
            </li>
          ))}
        </ul>
      </Section>

      {/* Como funciona */}
      <Section
        id="como-funciona"
        eyebrow="Como funciona"
        title='Três passos entre "não sei" e "sei exatamente".'
        className="bg-cream"
      >
        <div className="mt-14 grid gap-8 md:grid-cols-3">
          {STEPS.map((step, i) => (
            <div key={step.title}>
              <div className="relative flex h-10 w-10 items-center justify-center">
                <Hex className="absolute inset-0 h-full w-full fill-honey/15" />
                <span className="relative text-sm font-semibold text-honey-dark">
                  {i + 1}
                </span>
              </div>
              <h3 className="mt-4 text-lg font-semibold text-ink">
                {step.title}
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-ink-muted">
                {step.description}
              </p>
            </div>
          ))}
        </div>
      </Section>

      {/* Módulos */}
      <Section
        id="modulos"
        eyebrow="Módulos"
        title="Comece pelo estoque. Cresça pro financeiro."
        spacing="loose"
        className="bg-cream"
      >
        <div className="mt-14 grid items-start gap-6 md:grid-cols-3">
          {MODULES.map((mod) => {
            const styles = mod.active
              ? {
                  card: 'border-honey-dark/40 bg-cream-soft p-8 shadow-md shadow-honey/10',
                  badge: 'bg-honey/15 text-honey-dark',
                  title: 'text-lg font-semibold text-ink',
                }
              : {
                  card: 'border-dashed border-line p-6 opacity-70',
                  badge: 'bg-line/50 text-ink-muted',
                  title: 'text-base font-medium text-ink-muted',
                }

            return (
              <div key={mod.title} className={`relative rounded-2xl border ${styles.card}`}>
                <span
                  className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${styles.badge}`}
                >
                  {mod.status}
                </span>
                <h3 className={`mt-4 ${styles.title}`}>{mod.title}</h3>
                <p className="mt-3 text-sm leading-relaxed text-ink-muted">
                  {mod.description}
                </p>
              </div>
            )
          })}
        </div>
      </Section>

      {/* CTA final */}
      <section className="bg-honey">
        <div className="mx-auto max-w-6xl px-6 py-16 text-center">
          <p className="text-2xl font-bold tracking-tight text-hive sm:text-3xl">
            Não descubra o furo no estoque na hora de separar o pedido.
          </p>
          <Link
            to="/login?demo=1"
            className="mt-8 inline-flex items-center rounded-full bg-hive px-7 py-3 font-semibold text-cream shadow-lg transition hover:bg-hive-soft"
          >
            Acessar o sistema
          </Link>
        </div>
      </section>

      {/* ================= DIVISOR ================= */}
      <div
        id="tecnico"
        className="scroll-mt-20 border-y border-cream/10 bg-ink py-3 text-center text-xs font-medium uppercase tracking-widest text-cream/40"
      >
        A partir daqui, os bastidores técnicos do projeto
      </div>

      {/* ================= PARTE 2 — DETALHES TÉCNICOS ================= */}

      <Section
        eyebrow="Detalhes técnicos"
        title="Sem framework de ERP pronto por trás."
        className="bg-cream"
      >
        <p className="mt-4 max-w-2xl text-sm leading-relaxed text-ink-muted">
          Backend em .NET 10 com Minimal APIs, EF Core e SQL Server; frontend
          em React. Projeto pessoal, em desenvolvimento — e o estado abaixo é
          honesto: o que já está de pé e o que ainda falta.
        </p>

        <div className="mt-10 grid gap-10 md:grid-cols-2">
          <div>
            <h3 className="text-sm font-semibold text-ink-muted">Pronto</h3>
            <ul className="mt-4 space-y-3">
              {DONE.map((item) => (
                <li key={item} className="flex gap-3 text-sm leading-relaxed text-ink">
                  <span className="mt-0.5 shrink-0 text-honey-dark">✓</span>
                  {item}
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h3 className="text-sm font-semibold text-ink-muted">Próximo</h3>
            <ul className="mt-4 space-y-3">
              {NEXT.map((item) => (
                <li key={item} className="flex gap-3 text-sm leading-relaxed text-ink-muted">
                  <span className="mt-0.5 shrink-0 text-ink-muted/50">→</span>
                  {item}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </Section>

      {/* Decisões técnicas */}
      <Section
        eyebrow="Decisões técnicas"
        title="Por que foi construído assim."
        tone="dark"
        className="bg-hive text-cream"
      >
        <div className="mt-10 grid gap-8 md:grid-cols-2">
          {DECISIONS.map((item) => (
            <div key={item.title}>
              <h3 className="text-base font-semibold text-cream">
                {item.title}
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-cream/60">
                {item.description}
              </p>
            </div>
          ))}
        </div>
      </Section>

      {/* Stack técnica */}
      <Section eyebrow="Stack" title="Por que cada peça, e não outra." className="bg-cream-soft">
        <div className="mt-10 grid gap-8 sm:grid-cols-2">
          {STACK_DECISIONS.map((item) => (
            <div key={item.tech}>
              <h3 className="text-sm font-semibold text-ink">
                {item.tech}
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-ink-muted">
                {item.reason}
              </p>
            </div>
          ))}
        </div>
      </Section>

      {/* Assinatura */}
      <section className="border-b border-line bg-cream">
        <div className="mx-auto max-w-6xl px-6 py-14">
          <p className="max-w-2xl text-sm leading-relaxed text-ink-muted">
            Construído por{' '}
            <span className="font-semibold text-ink">Nicolas Fernandes</span>,
            desenvolvedor com background em ERP (Protheus/AdvPL) migrando
            para .NET e React. As regras de negócio aqui vêm de anos vendo
            estoque dar errado em sistema de verdade.
          </p>
          <div className="mt-4 flex gap-6 text-sm font-medium">
            <a
              href={LINKEDIN_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="text-honey-dark transition hover:text-ink"
            >
              LinkedIn
            </a>
            <a
              href={GITHUB_PROFILE_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="text-honey-dark transition hover:text-ink"
            >
              GitHub (perfil)
            </a>
          </div>
        </div>
      </section>

      {/* Footer / Links */}
      <footer className="bg-hive text-cream/70">
        <div className="mx-auto max-w-6xl px-6 py-14">
          <div className="flex flex-col items-start justify-between gap-8 sm:flex-row sm:items-center">
            <Logo />
            <div className="flex flex-wrap gap-x-8 gap-y-3 text-sm">
              <a
                href={GITHUB_URL}
                target="_blank"
                rel="noopener noreferrer"
                className="font-medium text-cream/80 transition hover:text-honey-light"
              >
                Repositório ↗
              </a>
              {SWAGGER_URL && (
                <a
                  href={SWAGGER_URL}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="font-medium text-cream/80 transition hover:text-honey-light"
                >
                  Documentação da API (Swagger) ↗
                </a>
              )}
            </div>
          </div>
          <p className="mt-10 text-xs text-cream/40">
            © 2026 Nicolas Fernandes · Distribuído sob licença MIT.
          </p>
        </div>
      </footer>
    </div>
  )
}

export default LandingPage
