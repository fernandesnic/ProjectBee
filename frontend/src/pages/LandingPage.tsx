import { Link } from 'react-router-dom'

const GITHUB_URL = 'https://github.com/fernandesnic/ProjectBee'
const SWAGGER_URL = 'http://localhost:5054/swagger'

const STACK = [
  '.NET 10',
  'ASP.NET Core',
  'Entity Framework Core',
  'SQL Server',
  'JWT',
  'FluentValidation',
  'React',
]

const DIFFERENTIALS = [
  {
    title: 'Chave composta',
    description:
      'Saldo não é um campo do produto — é uma entidade própria, com chave primária composta por produto, armazém e lote. O mesmo item existe em quantidades diferentes, em locais diferentes, sem perder rastreabilidade.',
  },
  {
    title: 'Validação assíncrona de SKU',
    description:
      'Cada SKU precisa ser único. A validação consulta o banco antes da escrita, prevenindo duplicidade sem depender só de uma constraint silenciosa.',
  },
  {
    title: 'Erros padronizados (RFC 7807)',
    description:
      'Toda falha da API responde no formato ProblemDetails — exceções não tratadas e falhas de validação seguem o mesmo contrato, previsível para quem consome.',
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
        <polygon points="24,3 43,14 43,34 24,45 5,34 5,14" fill="var(--color-hive)" />
        <polygon
          points="24,10 37,17.5 37,32.5 24,40 11,32.5 11,17.5"
          fill="none"
          stroke="var(--color-honey)"
          strokeWidth="2.5"
        />
        <circle cx="24" cy="25" r="4.5" fill="var(--color-honey)" />
      </svg>
      ProjectBee
    </span>
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
            <a
              href="#sobre"
              className="hidden text-sm text-ink-muted transition hover:text-ink sm:inline"
            >
              Sobre
            </a>
            <a
              href="#stack"
              className="hidden text-sm text-ink-muted transition hover:text-ink sm:inline"
            >
              Tecnologia
            </a>
            <Link
              to="/login"
              className="rounded-full border border-line px-4 py-1.5 text-sm font-medium text-ink transition hover:border-honey-dark hover:text-honey-dark"
            >
              Entrar
            </Link>
          </nav>
        </div>
      </header>

      {/* Hero */}
      <section className="relative overflow-hidden bg-hive text-cream">
        <Hex className="pointer-events-none absolute -right-16 -top-24 h-80 w-80 fill-none stroke-honey/15 stroke-[1.5]" />
        <Hex className="pointer-events-none absolute -bottom-28 -left-20 h-72 w-72 fill-none stroke-honey/10 stroke-[1.5]" />
        <Hex className="pointer-events-none absolute right-24 bottom-10 hidden h-16 w-16 fill-none stroke-honey/20 stroke-[2] lg:block" />

        <div className="relative mx-auto max-w-6xl px-6 py-28 sm:py-36">
          <span className="inline-flex items-center rounded-full border border-honey/30 bg-honey/10 px-3 py-1 text-xs font-medium tracking-wide text-honey-light">
            Sistema de gestão de estoque
          </span>

          <h1 className="mt-6 max-w-2xl text-4xl font-extrabold leading-[1.1] tracking-tight sm:text-6xl">
            O estoque não mente.
          </h1>

          <p className="mt-6 max-w-xl text-lg text-cream/70 sm:text-xl">
            ProjectBee controla o saldo de produtos distribuídos entre múltiplos
            armazéns, com rastreabilidade completa por lote.
          </p>

          <div className="mt-10 flex flex-wrap items-center gap-x-6 gap-y-3">
            <Link
              to="/login"
              className="inline-flex items-center rounded-full bg-honey px-7 py-3 font-semibold text-hive shadow-lg shadow-honey/20 transition hover:bg-honey-light"
            >
              Acessar sistema
            </Link>
            <a
              href={GITHUB_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="text-sm font-medium text-cream/60 underline decoration-cream/30 underline-offset-4 transition hover:text-cream"
            >
              Ver repositório no GitHub ↗
            </a>
          </div>
        </div>
      </section>

      {/* Sobre o problema */}
      <section id="sobre" className="border-b border-line bg-cream">
        <div className="mx-auto max-w-6xl px-6 py-24">
          <div className="grid gap-14 lg:grid-cols-2 lg:items-center">
            <div>
              <h2 className="text-sm font-semibold uppercase tracking-widest text-honey-dark">
                O problema
              </h2>
              <p className="mt-4 text-3xl font-bold leading-tight tracking-tight text-ink sm:text-4xl">
                Controlar estoque é simples até existir mais de um armazém.
              </p>
              <p className="mt-6 text-lg leading-relaxed text-ink-muted">
                A partir daí, o mesmo produto passa a existir em quantidades
                diferentes, em locais diferentes, muitas vezes com lotes e
                validades distintas. ProjectBee modela esse cenário desde a
                base: o saldo é rastreado por produto, armazém e lote — não um
                número solto associado ao item.
              </p>
            </div>

            <div className="flex flex-col items-center gap-3 rounded-2xl border border-line bg-cream-soft p-10 sm:flex-row sm:justify-center sm:gap-0">
              {(['Produto', 'Armazém', 'Lote'] as const).map((label, i) => (
                <div key={label} className="flex items-center gap-0 sm:contents">
                  <div className="relative flex h-24 w-24 shrink-0 items-center justify-center">
                    <Hex className="absolute inset-0 h-full w-full fill-hive/5 stroke-honey-dark stroke-[1.5]" />
                    <span className="relative text-sm font-semibold text-ink">
                      {label}
                    </span>
                  </div>
                  {i < 2 && (
                    <span className="mx-2 rotate-90 text-honey-dark sm:mx-4 sm:rotate-0">
                      →
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Stack técnica */}
      <section id="stack" className="border-b border-line bg-cream-soft">
        <div className="mx-auto max-w-6xl px-6 py-16">
          <p className="text-center text-sm font-medium text-ink-muted">
            Construído com tecnologia usada em produção
          </p>
          <ul className="mt-6 flex flex-wrap items-center justify-center gap-x-8 gap-y-4">
            {STACK.map((tech) => (
              <li
                key={tech}
                className="flex items-center gap-2 text-sm font-medium text-ink-muted"
              >
                <span className="h-1.5 w-1.5 rounded-full bg-honey-dark/60" />
                {tech}
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* Diferenciais */}
      <section id="diferenciais" className="bg-cream">
        <div className="mx-auto max-w-6xl px-6 py-24">
          <h2 className="text-sm font-semibold uppercase tracking-widest text-honey-dark">
            Diferenciais
          </h2>
          <p className="mt-4 max-w-xl text-3xl font-bold leading-tight tracking-tight text-ink sm:text-4xl">
            Regras que fazem o estoque valer confiança.
          </p>

          <div className="mt-14 grid gap-8 md:grid-cols-3">
            {DIFFERENTIALS.map((item, i) => (
              <div
                key={item.title}
                className="rounded-2xl border border-line bg-cream-soft p-8"
              >
                <span className="text-xs font-semibold text-honey-dark">
                  {String(i + 1).padStart(2, '0')}
                </span>
                <h3 className="mt-3 text-lg font-semibold text-ink">
                  {item.title}
                </h3>
                <p className="mt-3 text-sm leading-relaxed text-ink-muted">
                  {item.description}
                </p>
              </div>
            ))}
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
                GitHub ↗
              </a>
              <a
                href={SWAGGER_URL}
                className="font-medium text-cream/80 transition hover:text-honey-light"
              >
                Documentação da API (Swagger) ↗
                <span className="ml-1 text-xs font-normal text-cream/40">
                  requer execução local
                </span>
              </a>
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
