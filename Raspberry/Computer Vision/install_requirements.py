import subprocess


def install_requirements_with_poetry(requirements_file='requirements.txt'):
    with open(requirements_file, 'r') as f:
        for line in f:
            package = line.strip()
            if package and not package.startswith('#'):
                subprocess.run(['poetry', 'add', package], check=True)


install_requirements_with_poetry()
